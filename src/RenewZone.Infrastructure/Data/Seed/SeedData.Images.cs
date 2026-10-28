using Microsoft.EntityFrameworkCore;
using RenewZone.Domain.Entities;

namespace RenewZone.Infrastructure.Data.Seed;

/// <summary>
/// Gán ảnh THẬT (file trong wwwroot/uploads, đã đưa vào git) cho sản phẩm, banner, bài tin và
/// danh mục cha do <see cref="SeedData"/> tạo. Hàm seed gốc chỉ dùng ảnh placeholder, nên database
/// cài mới trên máy khác sẽ không có ảnh sản phẩm dù thư mục uploads có đủ file.
///
/// Danh sách ảnh lấy theo database của nhóm (RenewZoneClone, ngày 28/10/2026), khớp theo slug.
/// Banner không có slug nên khớp theo LinkUrl gốc của seed.
/// Idempotent: chỉ ghi vào dòng CÒN DÙNG ảnh placeholder — admin đã tự đổi ảnh thì không bị ghi đè.
/// </summary>
public static partial class SeedData
{
    /// <summary>Ảnh sản phẩm theo slug, ảnh đầu tiên là ảnh chính (IsMain).</summary>
    private static readonly Dictionary<string, string[]> SeedProductImages = new()
    {
        ["airpods-4"] =
        [
            "/uploads/products/2026-07/2ec58e72309545739f7a66133b4e8940.jpg",
        ],
        ["airpods-pro-2-usb-c"] =
        [
            "/uploads/products/2026-07/f32c98fb371c4b93a6e5c6b35c285f69.jpg",
            "/uploads/products/2026-07/86a667068d3148cca8de10d452c92ca7.jpg",
            "/uploads/products/2026-07/65761c9a85b14f9eb5bcf5801d8a20de.jpg",
        ],
        ["apple-watch-series-10-46mm-gps"] =
        [
            "/uploads/products/2026-07/14afa32ad915400b94298a02d1f44c39.jpg",
        ],
        ["apple-watch-ultra-2-49mm"] =
        [
            "/uploads/products/2026-07/9b55012ec25246788b7d66f18717576d.jpg",
            "/uploads/products/2026-07/e0bb212b09c042eda5f021dd893ec711.jpg",
            "/uploads/products/2026-07/edd4f5744d554e55b73c7ca0db43172f.jpg",
        ],
        ["ipad-air-m2-11-inch-wifi-128gb"] =
        [
            "/uploads/products/2026-07/f3dc54689ed44ada9a5864d438085c81.jpg",
            "/uploads/products/2026-07/4d9e7d9cbbfd4be08a5320a8f3a6a742.jpg",
            "/uploads/products/2026-07/b59ac5952ffe4c45b2d9b5b81da0d756.jpg",
            "/uploads/products/2026-07/048072213c4c45aebbd57a3a14290d49.jpg",
        ],
        ["ipad-gen-10-wifi-64gb"] =
        [
            "/uploads/products/2026-07/9725bfd635a046ad99753528e8efd243.jpg",
        ],
        ["ipad-mini-7-wifi-128gb"] =
        [
            "/uploads/products/2026-07/9a2dd7d013614cd393e816ea06e22156.jpg",
            "/uploads/products/2026-07/bf1db71a8719437fbafe16ef700f65d6.jpg",
            "/uploads/products/2026-07/382eedffb450400b912d439d3b30528d.jpg",
            "/uploads/products/2026-07/a58bab516c7c4eb5b8f53ebf7cf526f2.jpg",
            "/uploads/products/2026-07/881323e102fe489784d72479dd59dbe1.jpg",
            "/uploads/products/2026-07/e342209caff84c72a318070bfc8110d2.jpg",
        ],
        ["ipad-pro-m4-11-inch-wifi-256gb"] =
        [
            "/uploads/products/2026-07/d42be777c1a34d92a3a9d247b1169740.jpg",
            "/uploads/products/2026-07/e0c09f633a1a434798d7e6a91562aa75.jpg",
            "/uploads/products/2026-07/2dcd88197c514671bb9104bc7640b26e.jpg",
            "/uploads/products/2026-07/ebf22af065d444b6adbe743a0c3d523b.jpg",
        ],
        ["iphone-11-64gb"] =
        [
            "/uploads/products/2026-07/86e28e2b85e646269c5f159880efa957.jpg",
            "/uploads/products/2026-07/2c135613872c4dd081333c67dbbc4e84.jpg",
            "/uploads/products/2026-07/f6246d61716d4ce5a1452c9525faef05.jpg",
            "/uploads/products/2026-07/375272b5d1ea434fbc2f83a18444ce8f.jpg",
            "/uploads/products/2026-07/f18cd683643d4a8bbdfec1a3625807f3.jpg",
            "/uploads/products/2026-07/88bd20bc682f4299a904b737bd267128.jpg",
            "/uploads/products/2026-07/1857940287404400b1bdb1591eebccf7.jpg",
        ],
        ["iphone-12-64gb-thanh-ly"] =
        [
            "/uploads/products/2026-07/ff24a8ec34ae43b1b75c7bdac56ef791.jpg",
        ],
        ["iphone-13-128gb"] =
        [
            "/uploads/products/2026-07/1902023ea5f94bdfacc35754d9e88183.png",
            "/uploads/products/2026-07/ebd11c1b8388447d9e0883d47bfd421d.jpg",
            "/uploads/products/2026-07/d15964bce6ef4847b72bf848c0b399a6.jpg",
            "/uploads/products/2026-07/e2a15fd09de54a2b8f1048d896af0c48.jpg",
            "/uploads/products/2026-07/417fdb49cb344a7b8e0e9cfaf96769d5.jpg",
        ],
        ["iphone-14-pro-max-128gb"] =
        [
            "/uploads/products/2026-07/03ca59c3ec72467098fbb4baa8a58370.jpg",
            "/uploads/products/2026-07/f50f24a727354f80bad3009ed61cb3a9.jpg",
            "/uploads/products/2026-07/14b5c71afe4b4b38a17f7826bef6191d.jpg",
        ],
        ["iphone-15-128gb"] =
        [
            "/uploads/products/2026-07/6c97d33b82c74961a205274e6a78d908.jpg",
            "/uploads/products/2026-07/5c8a342dbb354da0ad561ee6d9b73719.jpg",
            "/uploads/products/2026-07/0a3c22bde2be4700957d49d8cdbb326b.jpg",
        ],
        ["iphone-15-pro-128gb"] =
        [
            "/uploads/products/2026-07/dff5755645b7484ba7b413efbbf2c2df.jpg",
            "/uploads/products/2026-07/7684bf2bac454485af4bd12647b8e466.jpg",
            "/uploads/products/2026-07/8e5142a463f2431488bf33f8219d875c.jpg",
        ],
        ["iphone-15-pro-max-256gb"] =
        [
            "/uploads/products/2026-07/559524e7f99146cda04e258329d80367.jpg",
            "/uploads/products/2026-07/e6fdadd7e4f94d67bdfb863477aea0c8.jpg",
            "/uploads/products/2026-07/c15d880f305943bda050fa3633dcccc2.jpg",
            "/uploads/products/2026-07/9feb79cbefa940b4a7a9dc52476bbed0.jpg",
            "/uploads/products/2026-07/89bb78d06791431cacd7131c5faffa68.jpg",
        ],
        ["iphone-16-pro-128gb"] =
        [
            "/uploads/products/2026-07/18bfc4f8b0df4c61b6d09104c634d76b.jpg",
            "/uploads/products/2026-07/e6d307fca83a44b5b0f193183c470fb3.jpg",
            "/uploads/products/2026-07/8f23b73040414d11b3bf896c3d500aaa.jpg",
            "/uploads/products/2026-07/d966344cd84e420ba417a908c8fc5afe.jpg",
            "/uploads/products/2026-07/1757060671664fb886a6de3666f3a0b9.jpg",
        ],
        ["iphone-16-pro-max-256gb"] =
        [
            "/uploads/products/2026-07/5d277b5e8bef42bcb2e9f6fb8bb22731.jpg",
            "/uploads/products/2026-07/407bd052404c4ec6a97b235255da7350.jpg",
            "/uploads/products/2026-07/6d78cc44d996459e95b86e856454b532.jpg",
            "/uploads/products/2026-07/717d61a8a87e422db6c4f45bbd94d8dc.jpg",
        ],
        ["iphone-17-pro-max-256gb"] =
        [
            "/uploads/products/2026-07/458a02769e4a4caab8a3d32fe990b03e.jpg",
            "/uploads/products/2026-07/1211241200fc42fe92d73386cb1914f7.jpg",
            "/uploads/products/2026-07/47021725796d42c99952367e873b3c5c.jpg",
            "/uploads/products/2026-07/bdc24fdc14604903bd780433d0d3c0ca.jpg",
            "/uploads/products/2026-07/f1b4192ab95c4560ad844889e68238ac.jpg",
        ],
        ["macbook-air-m1-13-inch-8gb-256gb-2020"] =
        [
            "/uploads/products/2026-07/5b834aef114148e5837a0d0370849c07.jpg",
            "/uploads/products/2026-07/84760961cf6847b5beeeae192bc4c53b.jpg",
            "/uploads/products/2026-07/723801177fed4a4884abacb996c9ad72.jpg",
            "/uploads/products/2026-07/b5aca52c816440d190a57f9fde5ee20f.jpg",
            "/uploads/products/2026-07/9efa52bacf914c069239a1735ab8b69b.jpg",
        ],
        ["macbook-air-m1-thanh-ly"] =
        [
            "/uploads/products/2026-07/09e47e7184644f19b3a9a9e82ceff613.jpg",
            "/uploads/products/2026-07/e19729f80c7b40e1b909ccc7ebb2c4a7.jpg",
        ],
        ["macbook-air-m3-15-inch-16gb-256gb"] =
        [
            "/uploads/products/2026-07/4e22a3d67e9c4df5ba437ee5aaa8baec.jpg",
            "/uploads/products/2026-07/644a24c915b64d9f9ac7e149277430e6.jpg",
            "/uploads/products/2026-07/2faf1120e4c04b7cb22c4e6e4f66594f.jpg",
            "/uploads/products/2026-07/5cb69ad22f54471aa16aad0dda3700f2.jpg",
        ],
        ["macbook-pro-m4-14-inch-16gb-512gb"] =
        [
            "/uploads/products/2026-07/47522ae7706c40db94e8bb0f08072289.jpg",
        ],
    };

    /// <summary>Ảnh banner theo LinkUrl của banner trong <c>SeedBannersAsync</c>.</summary>
    private static readonly Dictionary<string, string> SeedBannerImages = new()
    {
        ["/iphone-17-pro-max-256gb"] = "/uploads/editor/2026-07/aa9f87bbc20c4adda942a59b442d959a.jpg",
        ["/bang-gia-thu-cu-doi-moi"] = "/uploads/banners/2026-07/b04c7cb08da74acfaa2c97e1be8ccff8.jpg",
        ["/macbook"] = "/uploads/banners/2026-07/ce79a6a16b304e59894ecfae430e05bf.jpg",
        ["/ipad"] = "/uploads/banners/2026-07/85e29827dd6448f6afb67b3cdd388b18.jpg",
        ["/hang-thanh-ly"] = "/uploads/banners/2026-07/614b500852af473eb294e6f1e7faf26b.jpg",
    };

    /// <summary>Ảnh đại diện bài tin theo slug (2 bài còn lại nhóm chưa có ảnh, giữ placeholder).</summary>
    private static readonly Dictionary<string, string> SeedNewsImages = new()
    {
        ["iphone-17-pro-max-lo-dien-thiet-ke-moi"] = "/uploads/news/2026-07/2ec9f45ac69a41e9a961b73b59439853.jpg",
        ["5-meo-tiet-kiem-pin-iphone"] = "/uploads/news/2026-07/b02deb166b00472da0154114732555f4.jpg",
        ["nen-mua-iphone-15-pro-max-cu-hay-iphone-16-moi"] = "/uploads/news/2026-07/a94510b2986a4078bb9b6e64a783ef8a.jpg",
        ["apple-watch-theo-doi-suc-khoe-tim-mach"] = "/uploads/news/2026-07/12c2450f0f984631bb17e732f76a5ae4.jpg",
        ["huong-dan-mua-dien-thoai-tra-gop-0-phan-tram"] = "/uploads/news/2026-07/3694a3833e5d48f98d7a48c8f6474c1a.jpg",
        ["airpods-the-he-moi-do-nhip-tim"] = "/uploads/news/2026-07/6aa23833e17c4a07ac4e849764f66b05.jpg",
    };

    /// <summary>Ảnh danh mục cha (khối danh mục ở trang chủ) theo slug.</summary>
    private static readonly Dictionary<string, string> SeedCategoryImages = new()
    {
        ["iphone"] = "/uploads/editor/2026-07/417425f196704abfbdc0eb83ffc4ee6e.jpg",
        ["macbook"] = "/uploads/editor/2026-07/3a9a0aeb89c4492eb8e496271293b0b0.jpg",
        ["ipad"] = "/uploads/editor/2026-07/af4b9d334bf246aebbbf72b7be0fa367.jpg",
        ["apple-watch"] = "/uploads/editor/2026-07/b563dce25db04abdb421a1d05959ba71.jpg",
        ["airpods"] = "/uploads/editor/2026-07/f1ddf4de8846470f97ee29021070ff1a.jpg",
        ["hang-thanh-ly"] = "/uploads/editor/2026-07/c0b7d44e86c645d9b88b4a24ea23ace6.jpg",
    };

    private static async Task BackfillImagesAsync(AppDbContext db)
    {
        var changed = false;

        // Sản phẩm: chỉ thay khi mọi ảnh còn là placeholder của seed (admin đã tải ảnh nào lên thì bỏ qua cả sản phẩm)
        var productSlugs = SeedProductImages.Keys.ToList();
        var products = await db.Products
            .Include(p => p.Images)
            .Where(p => productSlugs.Contains(p.Slug)
                        && p.Images.Any()
                        && p.Images.All(i => i.Url == ProductImg))
            .ToListAsync();

        foreach (var product in products)
        {
            // ToList để không xóa trên chính collection đang duyệt; ảnh mới gắn qua ProductId
            db.ProductImages.RemoveRange(product.Images.ToList());

            var urls = SeedProductImages[product.Slug];
            for (var i = 0; i < urls.Length; i++)
            {
                db.ProductImages.Add(new ProductImage { ProductId = product.Id, Url = urls[i], IsMain = i == 0, DisplayOrder = i });
            }

            changed = true;
        }

        var banners = await db.Banners.Where(b => b.ImageUrl == BannerImg).ToListAsync();
        foreach (var banner in banners)
        {
            if (banner.LinkUrl is not null && SeedBannerImages.TryGetValue(banner.LinkUrl, out var url))
            {
                banner.ImageUrl = url;
                changed = true;
            }
        }

        var articles = await db.NewsArticles.Where(a => a.ThumbnailUrl == NewsImg).ToListAsync();
        foreach (var article in articles)
        {
            if (SeedNewsImages.TryGetValue(article.Slug, out var url))
            {
                article.ThumbnailUrl = url;
                changed = true;
            }
        }

        var categories = await db.Categories.Where(c => c.ImageUrl == ProductImg).ToListAsync();
        foreach (var category in categories)
        {
            if (SeedCategoryImages.TryGetValue(category.Slug, out var url))
            {
                category.ImageUrl = url;
                changed = true;
            }
        }

        if (changed) await db.SaveChangesAsync();
    }
}
