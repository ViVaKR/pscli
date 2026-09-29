# pscli

>- 어리석은 백성이 파와샬을 모르니..." 훈민정음 정신으로 만든 .NET Native AOT 기반 초고속 PowerShell 모듈/함수 템플릿 생성 CLI 도구

>- Ultra-fast PowerShell module &amp; function scaffolding CLI tool built with .NET Native AOT.


```bash

dotnet new console -o . -n pscli
pscli new module -n MyTools -o . --git -Author "Kim Bum Jun" -Description "My PowerShell toolkit"
dotnet run -- new module -n MyModule -o /tmp/pwsh-check --git -Author "Kim Bum Jun" -Description "My PowerShell toolkit"
pscli new function -n Create-Widget
pscli new function -n Get-Widget

```
