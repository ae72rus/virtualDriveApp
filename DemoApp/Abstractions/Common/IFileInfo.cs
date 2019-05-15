namespace DemoApp.Abstractions.Common
{
    public interface IFileInfo : IEntityInfo
    {
        long Length { get; }
    }
}