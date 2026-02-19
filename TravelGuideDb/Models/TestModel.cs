//Created by TestModelClassCreator at 7/24/2025 11:44:10 PM

using SystemTools.SystemToolsShared;

namespace TravelGuideDb.Models;

//ეს არის სატესტო მოდელი, რომელიც არის უბრალოდ ნიმუშისათვის და შესაძლებელია წაიშალოს საჭირების შემთხვევაში

public sealed class TestModel : ItemData
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public TestModel(string testName)
    {
        TestName = testName;
    }

    public int TestId { get; set; }
    public string TestName { get; set; }
}
