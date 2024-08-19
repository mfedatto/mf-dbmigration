Import-Module dbatools

$server = '%%extractor_sqlserver_server%%'
$database = '%%extractor_sqlserver_database%%'
$username = '%%extractor_sqlserver_username%%'
$password = '%%extractor_sqlserver_password%%'
$outputfile = '%%extractor_sqlserver_outputpath%%\versioned\V20240814.0.2__Create_triggers_and_indexes.sql'

$securePassword = ConvertTo-SecureString $password -AsPlainText -Force
$credential = New-Object System.Management.Automation.PSCredential($username, $securePassword)

New-Item -Path $outputfile -ItemType File -Force

Write-Host "Conectando ao servidor SQL..." -ForegroundColor Cyan
$serverInstance = Connect-DbaInstance -SqlInstance $server -SqlCredential $credential -TrustServerCertificate

if ($null -eq $serverInstance) {
    Write-Host "Falha ao conectar ao servidor SQL. Verifique as credenciais e o nome do servidor." -ForegroundColor Red
    exit
}

Write-Host "Conexão estabelecida com sucesso." -ForegroundColor Green

function Generate-Script {
    param (
        [string]$TypeName,
        [string]$Name,
        [string]$ScriptText
    )

    $script = @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = N'$Name' AND type = '$TypeName')
BEGIN
    $ScriptText
END`n
"@

    return $script
}

Write-Host "Iniciando extração e geração dos scripts de criação de triggers..." -ForegroundColor Cyan
$triggers = Get-DbaDbTrigger -SqlInstance $serverInstance -Database $database

if ($null -eq $triggers -or $triggers.Count -eq 0) {
    Write-Host "Nenhuma trigger encontrada no banco de dados '$database'." -ForegroundColor Red
} else {
    foreach ($trigger in $triggers) {
        Write-Host "Processando trigger '$($trigger.Name)'..." -ForegroundColor Yellow
        $triggerScript = $trigger.Script()

        if ($null -eq $triggerScript -or $triggerScript.Trim().Length -eq 0) {
            Write-Host "Aviso: Nenhum script gerado para a trigger '$($trigger.Name)'." -ForegroundColor Red
        } else {
            $scriptText = Generate-Script -TypeName 'TR' -Name $trigger.Name -ScriptText $triggerScript
            Add-Content -Path $outputfile -Value $scriptText
            Write-Host "Trigger '$($trigger.Name)' processada e script adicionado ao arquivo." -ForegroundColor Green
        }
    }
}

if ($null -eq $triggers -or $triggers.Count -eq 0) {
    Write-Host "Nenhuma trigger foi processada. Encerrando o processo de triggers." -ForegroundColor Yellow
}

Write-Host "Iniciando extração e geração dos scripts de criação de índices..." -ForegroundColor Cyan
$query = @"
SELECT 
    ix.name AS IndexName, 
    t.name AS TableName, 
    'CREATE INDEX ' + ix.name + ' ON ' + t.name + ' (' + 
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.index_column_id) + ')' AS CreateIndexScript
FROM 
    sys.indexes ix
    INNER JOIN sys.index_columns ic ON ix.object_id = ic.object_id AND ix.index_id = ic.index_id
    INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    INNER JOIN sys.tables t ON ix.object_id = t.object_id
WHERE 
    ix.type > 0
GROUP BY 
    ix.name, t.name
ORDER BY 
    t.name, ix.name;
"@

$indexes = Invoke-DbaQuery -SqlInstance $serverInstance -Database $database -Query $query

if ($null -eq $indexes -or $indexes.Count -eq 0) {
    Write-Host "Nenhum índice encontrado no banco de dados '$database'." -ForegroundColor Red
} else {
    foreach ($index in $indexes) {
        Write-Host "Processando índice '$($index.IndexName)' na tabela '$($index.TableName)'..." -ForegroundColor Yellow
        
        $scriptText = @"
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'$($index.IndexName)' AND object_id = OBJECT_ID(N'$($index.TableName)'))
BEGIN
    $($index.CreateIndexScript)
END`n
"@
        
        Add-Content -Path $outputfile -Value $scriptText
        Write-Host "Índice '$($index.IndexName)' processado e script adicionado ao arquivo." -ForegroundColor Green
    }
}

if ($null -eq $indexes -or $indexes.Count -eq 0) {
    Write-Host "Nenhum índice foi processado. Encerrando o processo de índices." -ForegroundColor Yellow
}

Write-Host "Processo concluído. Scripts gerados em '$outputfile'." -ForegroundColor Cyan
