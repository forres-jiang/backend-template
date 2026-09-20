using My.XXX.Shared;

namespace My.XXX.Service.Common;

public static class MenuErrors
{
    public static BusinessError InvalidInput() => Error("Menu.InvalidInput", "Invalid menu input.");
    public static BusinessError NotFound() => Error("Menu.NotFound", "Menu not found.");
    public static BusinessError InvalidParent() => Error("Menu.InvalidParent", "Invalid menu parent.");
    public static BusinessError HasChildren() => Error("Menu.HasChildren", "Remove child menus before deleting their parent.");
    public static BusinessError InvalidSelection() => Error("Menu.InvalidSelection", "Invalid role or menu selection.");
    public static BusinessError InvalidOrder() => Error("Menu.InvalidOrder", "Invalid menu order.");
    public static BusinessError WriteFailed() => Error("Menu.WriteFailed", "Save failed.");
    private static BusinessError Error(string code, string message) => new(message, code: code);
}
