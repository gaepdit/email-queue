using EmailQueue.API.Models;

namespace EmailQueue.API.Tests;

[TestFixture]
public class CreateEmailTaskTests
{
    private static EmailTask CreateEmailTask(List<string> recipients, List<string>? copies = null) =>
        EmailTask.Create(new NewEmailTask
        {
            From = "test-from@example.com",
            FromName = "From Name",
            Recipients = recipients,
            CopyRecipients = copies,
            Subject = "Subject",
            Body = "Body",
            IsHtml = false,
        }, batchId: Guid.NewGuid(), clientName: "Test Client", clientId: Guid.NewGuid(), counter: 1);

    [Test]
    public void RecipientsGetTrimmedAndDuplicatesGetRemoved()
    {
        // Arrange
        List<string> recipients = ["a@example.com ", " a@example.com", " b@example.net ", "b@example.net"];
        List<string> expected = ["a@example.com", "b@example.net"];

        // Act
        var result = CreateEmailTask(recipients);

        //Assert
        result.Recipients.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void EmptyRecipientsGetRemoved()
    {
        // Arrange
        List<string> recipients = ["r@example.com", ""];
        List<string> expected = ["r@example.com"];

        // Act
        var result = CreateEmailTask(recipients);

        //Assert
        result.Recipients.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void CopiesGetTrimmedAndDuplicatesGetRemoved()
    {
        // Arrange
        List<string> recipients = ["r@example.com"];
        List<string> copies = ["a@example.com ", " a@example.com", " b@example.net ", "b@example.net"];
        List<string> expected = ["a@example.com", "b@example.net"];

        // Act
        var result = CreateEmailTask(recipients, copies);

        //Assert
        result.CopyRecipients.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void EmptyCopiesGetRemoved()
    {
        // Arrange
        List<string> recipients = ["r@example.com"];
        List<string> copies = ["a@example.com", ""];
        List<string> expected = ["a@example.com"];

        // Act
        var result = CreateEmailTask(recipients, copies);

        //Assert
        result.CopyRecipients.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void IfAllCopiesAreEmpty_CopiesBecomesNull()
    {
        // Arrange
        List<string> recipients = ["r@example.com"];
        List<string> copies = [""];

        // Act
        var result = CreateEmailTask(recipients, copies);

        //Assert
        result.CopyRecipients.Should().BeEmpty();
    }

    [Test]
    public void SubjectAndFromAndFromNameGetTrimmed()
    {
        // Arrange and Act
        var result = EmailTask.Create(new NewEmailTask
        {
            From = " test-from@example.com ",
            FromName = " From Name ",
            Recipients = ["r@example.com"],
            CopyRecipients = null,
            Subject = " Subject ",
            Body = " Body ",
            IsHtml = false,
        }, batchId: Guid.NewGuid(), clientName: "Test Client", clientId: Guid.NewGuid(), counter: 1);

        //Assert
        using var scope = new AssertionScope();
        result.From.Should().Be("test-from@example.com");
        result.FromName.Should().Be("From Name");
        result.Subject.Should().Be("Subject");
        result.Body.Should().Be(" Body ");
        result.CopyRecipients.Should().BeNull();
    }
}
