#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    release.ps1 — pscli 릴리스 자동화 스크립트 (release.sh의 PowerShell 이식판)

.DESCRIPTION
    전제:
      - 이 스크립트는 개발 레포(pscli) 루트에서 실행한다.
      - 옆(형제) 디렉토리에 homebrew-pscli 레포가 클론되어 있다고 가정한다.
        디렉토리 구조 예시:
          GitWorkspace/
            ├── pscli/              (개발 레포, 이 스크립트가 여기서 실행됨)
            └── homebrew-pscli/     (탭 레포)
      - gh(GitHub CLI)가 설치되어 있고 로그인되어 있어야 GitHub Release 생성이 됨.
        없으면 tar.gz 파일만 만들고, 릴리스 업로드는 수동으로 안내한다.

    [bash → pwsh 번역 노트]
    - `set -euo pipefail`에 대응하는 자동 기능이 없음. PowerShell 자체 cmdlet 에러는
      $ErrorActionPreference = 'Stop'으로 멈추지만, git/dotnet/tar 같은 "네이티브 프로그램"은
      실패해도 pwsh가 자동으로 스크립트를 멈추지 않는다. 그래서 네이티브 명령 직후마다
      Assert-LastExitCode 헬퍼로 $LASTEXITCODE를 직접 검사한다. 이게 bash와 가장 다른 지점.

.EXAMPLE
    ./release.ps1 0.1.1
#>

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$NewVersion
)

$ErrorActionPreference = 'Stop'   # PowerShell 자체 cmdlet 에러는 이걸로 멈춤

# -----------------------------------------------------------------------
# 헬퍼: 네이티브 명령(git, dotnet, tar, gh 등) 실행 직후 종료 코드 검사
# bash의 `set -e`가 자동으로 해주던 일을 pwsh에서는 이렇게 수동으로 해줘야 함
# -----------------------------------------------------------------------
function Assert-LastExitCode {
    param([string]$Message = "명령이 실패했어.")
    if ($LASTEXITCODE -ne 0) {
        throw "$Message (exit code: $LASTEXITCODE)"
    }
}

# =========================================================================
$Tag = "v${NewVersion}"
$Rid = "osx-arm64"
$DevRepoDir = (Get-Location).Path
$TapRepoDir = (Resolve-Path (Join-Path $DevRepoDir "../homebrew-pscli")).Path
$AssetName = "pscli-${Tag}-${Rid}.tar.gz"
$GithubRepo = "ViVaKR/pscli"                 # 바이너리를 릴리스로 올릴 개발 레포
$TapGithubRepo = "ViVaKR/homebrew-pscli"

Write-Host "════════════════════════════════════════"
Write-Host " pscli 릴리스: $Tag"
Write-Host " 개발 레포:   $DevRepoDir"
Write-Host " 탭 레포:     $TapRepoDir"
Write-Host "════════════════════════════════════════"

# -----------------------------------------------------------------------
# 1. 개발 레포: 버전 번호 갱신 (csproj)
# -----------------------------------------------------------------------
$Csproj = Join-Path $DevRepoDir "pscli.csproj"
if (-not (Test-Path $Csproj)) {
    Write-Error "!! $Csproj 를 못 찾았네. 실행 위치(개발 레포 루트)를 확인해줘."
    exit 1
}

Write-Host "==> 1. csproj 버전을 $NewVersion 으로 갱신"
# [번역 노트] sed -i '' "s#<Version>.*</Version>#...#" 파일 →
#             Get-Content로 통째로 읽고 -replace(정규식)로 치환 후 Set-Content로 다시 씀
(Get-Content $Csproj -Raw) -replace '<Version>.*</Version>', "<Version>${NewVersion}</Version>" |
    Set-Content -Path $Csproj -NoNewline
Select-String -Path $Csproj -Pattern '<Version>' | ForEach-Object { Write-Host "    $($_.Line.Trim())" }

# -----------------------------------------------------------------------
# 2. AOT 게시(publish) — 단일 바이너리 생성
# -----------------------------------------------------------------------
Write-Host "==> 2. dotnet publish (AOT, $Rid)"
Set-Location $DevRepoDir
Remove-Item -Path "bin", "obj" -Recurse -Force -ErrorAction SilentlyContinue

dotnet publish -c Release -r $Rid -o "publish_out"
Assert-LastExitCode "dotnet publish 실패"

$BinPath = Join-Path "publish_out" "pscli"
if (-not (Test-Path $BinPath)) {
    Write-Error "!! $BinPath 가 생성되지 않았네. publish 로그를 확인해줘."
    exit 1
}

# -----------------------------------------------------------------------
# 3. tar.gz 압축 + sha256 계산
# -----------------------------------------------------------------------
Write-Host "==> 3. 압축 및 sha256 계산"
# [번역 노트] mktemp -d → New-Item으로 임시 디렉토리 직접 생성
$StageDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $StageDir | Out-Null

Copy-Item -Path $BinPath -Destination (Join-Path $StageDir "pscli")
& chmod +x (Join-Path $StageDir "pscli")

$AssetPath = Join-Path $DevRepoDir $AssetName
tar -czf $AssetPath -C $StageDir pscli
Assert-LastExitCode "tar 압축 실패"

Remove-Item -Path $StageDir -Recurse -Force

# [번역 노트] shasum -a 256 | awk '{print $1}' → Get-FileHash 로 대체 (외부 프로그램 2개 → cmdlet 1개)
$Sha256 = (Get-FileHash -Path $AssetPath -Algorithm SHA256).Hash.ToLower()
Write-Host "    자산 파일: $AssetName"
Write-Host "    sha256:    $Sha256"

# -----------------------------------------------------------------------
# 4. 개발 레포: 커밋 + 태그 + push
# -----------------------------------------------------------------------
Write-Host "==> 4. 개발 레포 커밋/태그/push"
git add $Csproj
Assert-LastExitCode

git commit -m "release: v${NewVersion}" -m "- pscli.csproj 버전을 ${NewVersion} 으로 갱신`n- ${Rid} 대상 AOT 바이너리 게시 준비"
Assert-LastExitCode "git commit 실패"

git tag -a $Tag -m "pscli $Tag"
Assert-LastExitCode
git push origin main
Assert-LastExitCode "git push 실패"
git push origin $Tag
Assert-LastExitCode

# -----------------------------------------------------------------------
# 5. GitHub Release 생성 + 바이너리 업로드 (gh CLI 있을 때만)
# -----------------------------------------------------------------------
# [번역 노트] command -v gh >/dev/null 2>&1 → Get-Command -ErrorAction SilentlyContinue
$GhAvailable = [bool](Get-Command gh -ErrorAction SilentlyContinue)

if ($GhAvailable) {
    Write-Host "==> 5. GitHub Release 생성 및 자산 업로드"
    gh release create $Tag $AssetPath `
        --repo $GithubRepo `
        --title "pscli $Tag" `
        --notes "pscli $Tag — 자동 릴리스 (release.ps1)"
    Assert-LastExitCode "gh release create 실패"
}
else {
    Write-Warning "!! gh CLI가 없어서 릴리스 업로드는 수동으로 해줘:"
    Write-Host "   1) https://github.com/$GithubRepo/releases/new 에서 태그 $Tag 선택"
    Write-Host "   2) $AssetPath 파일을 자산으로 첨부 후 게시"
    # [번역 노트] read -q (zsh 전용 y/n 프롬프트) → Read-Host 로 단순화
    Read-Host "   업로드 완료했으면 Enter, 중단하려면 Ctrl+C"
}

# -----------------------------------------------------------------------
# 6. 탭 레포: pscli.rb 갱신
# -----------------------------------------------------------------------
Write-Host "==> 6. homebrew-pscli 레포의 pscli.rb 갱신"
if (-not (Test-Path $TapRepoDir)) {
    Write-Error "!! $TapRepoDir 를 못 찾았네. 탭 레포 경로를 확인해줘."
    exit 1
}

$RbFile = Join-Path $TapRepoDir "pscli.rb"
$NewUrl = "https://github.com/$GithubRepo/releases/download/$Tag/$AssetName"

# [번역 노트] cat > file << EOF ... EOF (heredoc) → here-string(@"..."@)
# bash의 ${VAR} 보간과 동일하게 pwsh here-string 안에서도 "$Var" 그대로 치환됨
$RbContent = @"
class Pscli < Formula
  desc "Ultra-fast PowerShell module & function scaffolding CLI tool built with .NET Native AOT"
  homepage "https://github.com/$GithubRepo"
  url "$NewUrl"
  sha256 "$Sha256"
  version "$NewVersion"

  def install
    bin.install "pscli"
  end
end
"@

Set-Content -Path $RbFile -Value $RbContent -NoNewline

Write-Host "    $RbFile 갱신 완료:"
Get-Content $RbFile | ForEach-Object { Write-Host "    $_" }

# -----------------------------------------------------------------------
# 7. 탭 레포: 커밋 + push
# -----------------------------------------------------------------------
Write-Host "==> 7. 탭 레포 커밋/push 및 v${NewVersion} 태그/릴리스 자동 선포"
Set-Location $TapRepoDir

# 혹시 로컬에 예전 작업 중 꼬인 동일 태그가 있다면 우회 및 클린 청소
git tag -d $Tag 2>$null

git add pscli.rb
Assert-LastExitCode

git commit -m "chore: bump pscli to ${Tag}" -m "- url/sha256/version 을 ${Tag} 릴리스 자산 기준으로 갱신`n- 자산: ${AssetName}`n- sha256: ${Sha256}"
Assert-LastExitCode "탭 레포 git commit 실패"

# 🎯 탭 레포 원격지 main 브랜치 전방 압송 푸시
git push origin main
Assert-LastExitCode

git tag -a $Tag -m "homebrew-pscli $Tag 릴리스"
Assert-LastExitCode
git push origin $Tag
Assert-LastExitCode

if ($GhAvailable) {
    gh release create $Tag `
        --repo $TapGithubRepo `
        --target main `
        --title "pscli $Tag" `
        --notes "homebrew-pscli $Tag — Homebrew 공식 자산 갱신 완료"
    Assert-LastExitCode "탭 레포 gh release create 실패"
}

# -----------------------------------------------------------------------
# 8. 정리 + 검증 안내
# -----------------------------------------------------------------------
Set-Location $DevRepoDir
Remove-Item -Path $AssetPath -Force -ErrorAction SilentlyContinue

Write-Host "════════════════════════════════════════"
Write-Host " 릴리스 $Tag 완료!"
Write-Host ""
Write-Host " 검증:"
Write-Host "   brew uninstall pscli 2>`$null; brew untap $TapGithubRepo 2>`$null"
Write-Host "   brew tap $TapGithubRepo"
Write-Host "   brew install pscli"
Write-Host "   pscli --version   # $NewVersion 이 나오는지 확인"
Write-Host "════════════════════════════════════════"
