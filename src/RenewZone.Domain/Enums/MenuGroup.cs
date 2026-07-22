namespace RenewZone.Domain.Enums;

/// <summary>Vị trí của một mục menu trên layout — quyết định nó render ở đâu.</summary>
public enum MenuGroup
{
    /// <summary>Thanh điều hướng chính (desktop) + offcanvas mobile.</summary>
    MainMenu = 0,

    /// <summary>Cột "Chính sách" ở footer.</summary>
    FooterPolicy = 1,

    /// <summary>Cột "Hỗ trợ" ở footer.</summary>
    FooterSupport = 2
}
