# services/compose.yml's SQL Server, on Windows: SQL Server Express with sa / Smoke_test_1234 (test-only) on
# 127.0.0.1:14330, where the overlays' connection strings look for it. For a throwaway machine such as a CI runner,
# where the compose file's Linux containers don't run. Also installs sqlcmd, which wfc.cs's setup uses.
$ErrorActionPreference = 'Stop'
$password = 'Smoke_test_1234'

choco install sql-server-express sqlcmd -y --no-progress
if ($LASTEXITCODE) { throw "choco install failed ($LASTEXITCODE)" }
$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + $env:Path

# Mixed-mode logins and a fixed TCP port, then a restart for both to take effect.
$instance = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL').SQLEXPRESS
$server = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$instance\MSSQLServer"
Set-ItemProperty $server -Name LoginMode -Value 2
Set-ItemProperty "$server\SuperSocketNetLib\Tcp" -Name Enabled -Value 1
Set-ItemProperty "$server\SuperSocketNetLib\Tcp\IPAll" -Name TcpPort -Value '14330'
Set-ItemProperty "$server\SuperSocketNetLib\Tcp\IPAll" -Name TcpDynamicPorts -Value ''
Restart-Service 'MSSQL$SQLEXPRESS' -Force

# The runner's account is a Windows administrator, so a Windows login sets sa's password.
$deadline = (Get-Date).AddMinutes(3)
while ($true) {
    sqlcmd -E -C -S '127.0.0.1,14330' -b -Q "ALTER LOGIN sa WITH PASSWORD = '$password', CHECK_POLICY = OFF; ALTER LOGIN sa ENABLE"
    if (-not $LASTEXITCODE) { break }
    if ((Get-Date) -gt $deadline) { throw 'SQL Server did not accept the login' }
    Start-Sleep -Seconds 5
}
sqlcmd -C -S '127.0.0.1,14330' -U sa -P $password -b -Q 'SELECT @@VERSION'
if ($LASTEXITCODE) { throw "sa can't log in ($LASTEXITCODE)" }
