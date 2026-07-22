using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;
using RenewZone.Domain.Interfaces;
using RenewZone.Domain.Models.Admin;
using RenewZone.Infrastructure.Data;

namespace RenewZone.Infrastructure.Services;

/// <summary>Xem <see cref="IContentBlockAdminService"/>.</summary>
public class ContentBlockAdminService(AppDbContext db) : IContentBlockAdminService
{
    public async Task<IReadOnlyList<ContentBlock>> GetByGroupAsync(string groupKey) =>
        await db.ContentBlocks.AsNoTracking()
            .Where(b => b.GroupKey == groupKey)
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync();

    public async Task<ContentBlock?> GetAsync(int id) =>
        await db.ContentBlocks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);

    public async Task<AdminResult> SaveAsync(ContentBlockInput input)
    {
        // Whitelist nhóm: form bị sửa tay không được tạo nhóm rác mà web khách không đọc tới
        string[] knownGroups =
        [
            ContentBlockGroups.ProductCommitments,
            ContentBlockGroups.CartAssurance,
            ContentBlockGroups.ContactChannels
        ];

        if (!knownGroups.Contains(input.GroupKey))
        {
            return AdminResult.Fail("Nhóm khối nội dung không hợp lệ.");
        }

        var block = input.Id is { } id
            ? await db.ContentBlocks.FirstOrDefaultAsync(b => b.Id == id)
            : null;

        if (input.Id is not null && block is null)
        {
            return AdminResult.Fail("Không tìm thấy khối nội dung.");
        }

        if (block is null)
        {
            block = new ContentBlock();
            db.ContentBlocks.Add(block);
        }

        block.GroupKey = input.GroupKey;
        block.Icon = string.IsNullOrWhiteSpace(input.Icon) ? null : input.Icon.Trim();
        block.Title = input.Title.Trim();
        block.Subtitle = input.Subtitle;
        block.LinkUrl = string.IsNullOrWhiteSpace(input.LinkUrl) ? null : input.LinkUrl.Trim();
        block.ImageUrl = input.ImageUrl;
        block.DisplayOrder = input.DisplayOrder;
        block.IsActive = input.IsActive;

        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã lưu khối “{block.Title}”.", block.Id);
    }

    public async Task<AdminResult> DeleteAsync(int id)
    {
        var block = await db.ContentBlocks.FirstOrDefaultAsync(b => b.Id == id);

        if (block is null)
        {
            return AdminResult.Fail("Không tìm thấy khối nội dung.");
        }

        db.ContentBlocks.Remove(block);
        await db.SaveChangesAsync();

        return AdminResult.Success($"Đã chuyển khối “{block.Title}” vào Thùng rác.");
    }

    public async Task<AdminResult> ToggleActiveAsync(int id)
    {
        var block = await db.ContentBlocks.FirstOrDefaultAsync(b => b.Id == id);

        if (block is null)
        {
            return AdminResult.Fail("Không tìm thấy khối nội dung.");
        }

        block.IsActive = !block.IsActive;
        await db.SaveChangesAsync();

        // Nhóm không còn khối nào đang bật ⇒ web khách ẨN CẢ KHỐI (luật fallback mục 4.7),
        // nói trước cho admin biết thay vì để họ tự phát hiện
        var remaining = await db.ContentBlocks
            .CountAsync(b => b.GroupKey == block.GroupKey && b.IsActive);

        var note = remaining == 0
            ? " Nhóm này không còn mục nào đang hiện — khối sẽ ẩn hẳn trên website."
            : "";

        return AdminResult.Success(
            (block.IsActive ? $"Đã hiện khối “{block.Title}”." : $"Đã ẩn khối “{block.Title}”.") + note);
    }

    public async Task<AdminResult> ReorderAsync(int[] orderedIds)
    {
        var blocks = await db.ContentBlocks.Where(b => orderedIds.Contains(b.Id)).ToListAsync();

        for (var i = 0; i < orderedIds.Length; i++)
        {
            var block = blocks.FirstOrDefault(b => b.Id == orderedIds[i]);
            if (block is not null)
            {
                block.DisplayOrder = i;
            }
        }

        await db.SaveChangesAsync();

        return AdminResult.Success("Đã lưu thứ tự.");
    }
}
