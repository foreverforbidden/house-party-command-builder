using System.IO;
using HpCommander.Services;
using Xunit;

namespace HpCommander.Builders.Tests;

/// <summary>
/// The writability probe is the gate in front of a ~60 MB download and an in-place file swap. If it
/// wrongly says yes, the user waits for a download that cannot be applied; if it wrongly says no,
/// a perfectly updatable install is sent to the browser instead.
/// </summary>
public class UpdateServiceTests
{
    [Fact]
    public void AnOrdinaryWritableFolderIsUpdatable()
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Assert.True(UpdateService.IsWritable(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void TheProbeLeavesNothingBehind()
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            UpdateService.IsWritable(directory);

            Assert.Empty(Directory.EnumerateFileSystemEntries(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AFolderThatCannotBeWrittenIsNotUpdatable()
    {
        // A path nested under a *file* can never be written to, which stands in for the real case
        // (an install under Program Files) without needing ACL surgery or elevation.
        var file = Path.GetTempFileName();
        try
        {
            Assert.False(UpdateService.IsWritable(Path.Combine(file, "nested")));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void AFolderThatDoesNotExistIsNotUpdatable()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"hpcommander-missing-{Guid.NewGuid():N}");

        Assert.False(UpdateService.IsWritable(missing));
    }
}
