$client = New-Object System.Net.WebClient
$client.Headers.Add('User-Agent', 'Mozilla/5.0')
$html = $client.DownloadString('https://bitmidi.com/search?q=zelda')
$html -split '\n' | Where-Object { $_ -match 'href=.*-mid' } | Select-Object -First 5
