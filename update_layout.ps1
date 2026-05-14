$path = 'c:\Welber\Github\corte_cor_ag.git\Pages\Shared\_Layout.cshtml';
$txt = Get-Content $path -Raw;
$target = '<li><a class="dropdown-item" asp-area="" asp-page="/Agendamentos2">Agendamentos</a></li>';
$replace = '<li><a class="dropdown-item" asp-area="" asp-page="/Agendamentos2">Agendamentos</a></li>' + [Environment]::NewLine + '                                    <li><a class="dropdown-item" asp-area="" asp-page="/AgendamentosLista">Lista de Agendamentos</a></li>';
$newTxt = $txt.Replace($target, $replace);
if ($newTxt -eq $txt) { Write-Host "Target not found"; exit 1 }
Set-Content $path $newTxt -Encoding UTF8;
Write-Host "Success";
