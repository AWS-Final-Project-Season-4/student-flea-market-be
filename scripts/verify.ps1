$ErrorActionPreference = 'Stop'

dotnet tool restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# A single MSBuild node avoids package-assets cache contention on Windows.
dotnet msbuild StudentFleaMarket.sln /t:Restore /m:1 /v:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet msbuild StudentFleaMarket.sln /t:Build /m:1 /p:Restore=false /v:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet test StudentFleaMarket.Tests/StudentFleaMarket.Tests.csproj --no-build --no-restore --verbosity:minimal
exit $LASTEXITCODE
