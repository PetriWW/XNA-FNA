$outputFile = "ProjectCodebase.txt"
if (Test-Path $outputFile) { Remove-Item $outputFile }

# Get the current directory path for relative calculations
$root = Get-Location

Get-ChildItem -Recurse -Filter *.cs | Where-Object { 
    $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\" 
} | ForEach-Object {
    # Resolve the relative path from the root
    $relativePath = Resolve-Path -Path $_.FullName -Relative
    
    # Clean the path string to remove leading '.\' for a cleaner look
    $cleanPath = $relativePath.Replace(".\", "")
    
    "--- FILE: $cleanPath ---" | Out-File -FilePath $outputFile -Append -Encoding utf8
    
    $content = [System.IO.File]::ReadAllText($_.FullName)
    $content | Out-File -FilePath $outputFile -Append -Encoding utf8
    "`n" | Out-File -FilePath $outputFile -Append
}