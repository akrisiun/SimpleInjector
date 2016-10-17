@cd src
dotnet restore
cd SimpleInjector\
dotnet pack
@cd ..\

cd SimpleInjector.Integration.Web.Mvc\
dotnet pack
@cd ..\

set msbuild="%ProgramFiles(x86)%\msbuild\14.0\Bin\msbuild.exe"
if not exist %msbuild% @set msbuild="%ProgramFiles%\msbuild\14.0\Bin\msbuild.exe"
if not exist %msbuild% @set msbuild="%ProgramFiles(x86)%\msbuild\12.0\Bin\msbuild.exe"
if not exist %msbuild% @set msbuild="%ProgramFiles%\msbuild\12.0\Bin\msbuild.exe"

%msbuild% /v:m SimpleInjector.NET\SimpleInjector.NET.csproj
%msbuild% /v:m SimpleInjector.Integration.Web.Mvc\SimpleInjector.Integration.Web.Mvc.csproj 

cd ..\
@PAUSE