using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IIdentitySubGroupRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class IdentitySubGroupRepository : IIdentitySubGroupRepository
{
    private readonly LegacyDbContext _dbContext;

    public IdentitySubGroupRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_IDENTITYSUBGRP identitySubGroup, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_IDENTITYSUBGRPs.AddAsync(identitySubGroup, cancellationToken);
    }

    public async Task<TB_IDENTITYSUBGRP?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_IDENTITYSUBGRPs
            .FirstOrDefaultAsync(s => s.ID == id, cancellationToken);
    }
}
