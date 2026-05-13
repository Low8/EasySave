using EasySave.ConsoleApp;

namespace EasySave.Tests;

public class CommandLineParserTests
{
    private readonly CommandLineParser _parser = new();

    [Fact]
    public void Parse_Range_ReturnsSequence()
        => Assert.Equal([1, 2, 3], _parser.Parse(["1-3"]).ToList());

    [Fact]
    public void Parse_MultipleArgs_ReturnsEach()
        => Assert.Equal([1, 3], _parser.Parse(["1", "3"]).ToList());

    [Fact]
    public void Parse_SingleIndex_ReturnsSingle()
        => Assert.Equal([1], _parser.Parse(["1"]).ToList());

    [Fact]
    public void Parse_SingleElementRange_ReturnsSingle()
        => Assert.Equal([1], _parser.Parse(["1-1"]).ToList());

    [Fact]
    public void Parse_EmptyArgs_ReturnsEmpty()
        => Assert.Empty(_parser.Parse([]).ToList());

    [Fact]
    public void Parse_NonNumeric_ReturnsEmpty()
        => Assert.Empty(_parser.Parse(["abc"]).ToList());

    [Fact]
    public void Parse_IndexZero_ReturnsZero()
        => Assert.Equal([0], _parser.Parse(["0"]).ToList());

    [Fact]
    public void Parse_LargeIndex_ReturnsIt()
        => Assert.Equal([6], _parser.Parse(["6"]).ToList());

    [Fact]
    public void Parse_SemicolonInArg_ReturnsRangeAndSingle()
        => Assert.Equal([1, 2, 3, 5], _parser.Parse(["1-3;5"]).ToList());
}
