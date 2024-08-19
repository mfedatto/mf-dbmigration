Import-Module dbatools

$server = '%%extractor_sqlserver_server%%'
$database = '%%extractor_sqlserver_database%%'
$username = '%%extractor_sqlserver_username%%'
$password = '%%extractor_sqlserver_password%%'
$outputfile = '%%extractor_sqlserver_outputpath%%\versioned\V20240814.0.1__Create_tables_and_constraints.sql'

$securePassword = ConvertTo-SecureString $password -AsPlainText -Force
$credential = New-Object System.Management.Automation.PSCredential($username, $securePassword)

New-Item -Path $outputfile -ItemType File -Force

Write-Host "Conectando ao servidor SQL..." -ForegroundColor Cyan
$serverInstance = Connect-DbaInstance -SqlInstance $server -SqlCredential $credential -TrustServerCertificate

if ($null -eq $serverInstance) {
    Write-Host "Falha ao conectar ao servidor SQL. Verifique as credenciais e o nome do servidor." -ForegroundColor Red
    exit
}

Write-Host "Conexao estabelecida com sucesso." -ForegroundColor Green
Write-Host "Iniciando extracao e geracao dos scripts de criacao de tabelas..." -ForegroundColor Cyan

$tables = Get-DbaDbTable -SqlInstance $serverInstance -Database $database

if ($null -eq $tables -or $tables.Count -eq 0) {
    Write-Host "Nenhuma tabela encontrada no banco de dados '$database'. Verifique se o banco de dados existe e contem tabelas." -ForegroundColor Cyan
    exit
}

Write-Host "$($tables.Count) tabelas encontradas. Gerando scripts de criacao de tabelas..." -ForegroundColor Cyan

$controlVariables = ($tables | ForEach-Object { "DECLARE @$($_.Name)_created BIT = 0;" }) + "`n"
Add-Content -Path $outputfile -Value $controlVariables

function Generate-TableScript {
    param (
        [string]$TableName,
        [string]$ScriptText
    )

    $script = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'$TableName')
BEGIN
    $ScriptText
    SET @$($TableName)_created = 1;
END`n
"@

    return $script
}

foreach ($table in $tables) {
    Write-Host "Processando tabela '$($table.Name)'..." -ForegroundColor Yellow
    $tableScript = $table.Script()
    
    if ($null -eq $tableScript -or $tableScript.Trim().Length -eq 0) {
        Write-Host "Aviso: Nenhum script gerado para a tabela '$($table.Name)'." -ForegroundColor Cyan
    } else {
        $scriptText = Generate-TableScript -TableName $table.Name -ScriptText $tableScript
        Add-Content -Path $outputfile -Value $scriptText
        Write-Host "Tabela '$($table.Name)' processada e script adicionado ao arquivo." -ForegroundColor Green
    }
}

Write-Host "Gerando scripts de criacao de indices..." -ForegroundColor Cyan

function Generate-IndexScript {
    param (
        [string]$TableName,
        [string]$IndexName,
        [string]$ScriptText
    )

    $script = @"
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'$IndexName' AND object_id = OBJECT_ID(N'$TableName'))
BEGIN
    $ScriptText
END`n
"@

    return $script
}

foreach ($table in $tables) {
    $indexQuery = @"
        SELECT name, object_id
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'$($table.Name)')
    "@
    $indexes = Invoke-DbaQuery -SqlInstance $serverInstance -Database $database -Query $indexQuery

    if ($null -eq $indexes -or $indexes.Count -eq 0) {
        Write-Host "Nenhum indice encontrado para a tabela '$($table.Name)'." -ForegroundColor Cyan
    } else {
        foreach ($index in $indexes) {
            Write-Host "Processando indice '$($index.name)'..." -ForegroundColor Yellow
            # Substitua esta linha por sua logica de geracao de script de indices
            $indexScript = "CREATE INDEX $($index.name) ON $($table.Name) (COLUMN_NAME);" # Ajuste conforme necessario

            if ($null -eq $indexScript -or $indexScript.Trim().Length -eq 0) {
                Write-Host "Aviso: Nenhum script gerado para o indice '$($index.name)'." -ForegroundColor Cyan
            } else {
                $scriptText = Generate-IndexScript -TableName $table.Name -IndexName $index.name -ScriptText $indexScript
                Add-Content -Path $outputfile -Value $scriptText
                Write-Host "Indice '$($index.name)' processado e script adicionado ao arquivo." -ForegroundColor Green
            }
        }
    }
}

Write-Host "Gerando scripts de criacao de constraints..." -ForegroundColor Cyan

function Generate-ConstraintScript {
    param (
        [string]$TableName,
        [string]$ScriptText
    )

    $script = @"
IF EXISTS (SELECT * FROM sys.tables WHERE name = N'$TableName') AND (@$($TableName)_created = 1 OR @$($TableName)_created IS NULL)
BEGIN
    $ScriptText
END`n
"@

    return $script
}

function Generate-FkConstraintScript {
    param (
        [string]$FkTableName,
        [string]$ReferencedTableName,
        [string]$ScriptText
    )

    $script = @"
IF EXISTS (SELECT * FROM sys.tables WHERE name = N'$FkTableName') AND EXISTS (SELECT * FROM sys.tables WHERE name = N'$ReferencedTableName') AND 
    (@$($FkTableName)_created = 1 OR @$($FkTableName)_created IS NULL OR @$($ReferencedTableName)_created = 1 OR @$($ReferencedTableName)_created IS NULL)
BEGIN
    $ScriptText
END`n
"@

    return $script
}

foreach ($table in $tables) {
    foreach ($constraint in $table.Constraints) {
        Write-Host "Processando constraint '$($constraint.Name)' para a tabela '$($table.Name)'..." -ForegroundColor Yellow
        $constraintScript = $constraint.Script()

        if ($null -eq $constraintScript -or $constraintScript.Trim().Length -eq 0) {
            Write-Host "Aviso: Nenhum script gerado para a constraint '$($constraint.Name)'." -ForegroundColor Cyan
        } else {
            if ($constraint.Type -eq 'FOREIGN KEY') {
                $referencedTable = $constraint.ReferencedTable
                $scriptText = Generate-FkConstraintScript -FkTableName $table.Name -ReferencedTableName $referencedTable -ScriptText $constraintScript
            } else {
                $scriptText = Generate-ConstraintScript -TableName $table.Name -ScriptText $constraintScript
            }

            Add-Content -Path $outputfile -Value $scriptText
            Write-Host "Constraint '$($constraint.Name)' processada e script adicionado ao arquivo." -ForegroundColor Green
        }
    }
}

Write-Host "Processo concluido. Scripts gerados em '$outputfile'." -ForegroundColor Cyan
