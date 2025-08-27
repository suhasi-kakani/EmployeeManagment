using Microsoft.Azure.Cosmos;
using Moq;

public static class MockCosmosHelper
{
    public static FeedIterator<T> CreateMockFeedIterator<T>(IEnumerable<T> items)
    {
        var feedIteratorMock = new Mock<FeedIterator<T>>();
        var hasMoreResults = true;

        feedIteratorMock.SetupSequence(f => f.HasMoreResults)
            .Returns(true)
            .Returns(false);

        feedIteratorMock.Setup(f => f.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<FeedResponse<T>>(r => r.GetEnumerator() == items.GetEnumerator()));

        return feedIteratorMock.Object;
    }
}