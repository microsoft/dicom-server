$Identity = @( Get-ChildItem -Path "$PSScriptRoot\Identity\*.ps1" )

@($Identity) | ForEach-Object {
    Try {
        . $_.FullName
    } Catch {
        Write-Error -Message "Failed to import function $($_.FullName): $_"
    }
}
