using NEXUS_WindowsForms.Services;
using Xunit;

namespace NEXUS_WindowsForms.Tests;

public class CadetNameValidatorTests
{
    [Fact]
    public void Normalize_NameWith20Characters_ReturnsTrimmedName()
    {
        string name = new('A', 20);

        string result = CadetNameValidator.Normalize(name);

        Assert.Equal(name, result);
    }

    [Fact]
    public void Normalize_NameWith60Characters_ReturnsTrimmedName()
    {
        string name = new('B', 60);

        string result = CadetNameValidator.Normalize(name);

        Assert.Equal(name, result);
    }

    [Fact]
    public void Normalize_NameWith61Characters_ThrowsArgumentException()
    {
        string name = new('C', 61);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CadetNameValidator.Normalize(name));

        Assert.Equal("nombreCadete", exception.ParamName);
    }

    [Fact]
    public void Normalize_EmptyName_ReturnsDefaultCadetName()
    {
        string result = CadetNameValidator.Normalize(string.Empty);

        Assert.Equal("Cadete Vance", result);
    }

    [Fact]
    public void Normalize_WhitespaceName_ReturnsDefaultCadetName()
    {
        string result = CadetNameValidator.Normalize("   \t  ");

        Assert.Equal("Cadete Vance", result);
    }
}
