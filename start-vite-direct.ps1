$env:Path = [System.Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path', 'User')
Set-Location H:/DataForge/frontend
# Add local node_modules/.bin to PATH
$env:Path = "H:\DataForge\frontend\node_modules\.bin;$env:Path"
# Run vite directly
& "H:\DataForge\frontend\node_modules\.bin\vite.ps1" --port 9999
