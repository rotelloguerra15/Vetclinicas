PS C:\Projetos\vetclinica> Get-Content C:\Projetos\vetclinica\VetClinica.API\Services\ProvisionamentoService.cs | Select-Object -Index (59..79)
            SchemaName = schema,
            CriadoEm  = DateTime.UtcNow,
            Ativo      = true
        };
        _platform.Tenants.Add(tenant);
        await _platform.SaveChangesAsync();

        // 4. Seeds dentro do schema recÃ©m-criado
        using var db = _factory.CreateForSchema(schema);

        // Replica o registro do tenant no schema do tenant
        // (para queries cross-schema em controllers como GestaoVistaController)
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var senhaTemp = GerarSenhaTemporaria();
        var owner = new User
        {
            Id        = Guid.NewGuid(),
            TenantId  = tenant.Id,
            Nome      = nomeDono,
PS C:\Projetos\vetclinica>