using DevOps.WebAPI.Services;
using Xunit;
using Assert = Xunit.Assert;

namespace TestProject1;

public class CalculatorServiceTests
{
    private readonly CalculatorService _sut = new();

    [Fact]
    public void Add_TwoPositiveNumbers_ReturnsSum()
        => Assert.Equal(5, _sut.Add(2, 3));

    [Fact]
    public void Subtract_LargerMinusSmaller_ReturnsPositive()
        => Assert.Equal(6, _sut.Subtract(10, 4));

    [Fact]
    public void Multiply_TwoNumbers_ReturnsProduct()
        => Assert.Equal(20, _sut.Multiply(4, 5));

    [Fact]
    public void Divide_TwoNumbers_ReturnsQuotient()
        => Assert.Equal(2.5, _sut.Divide(10, 4));

    [Fact]
    public void Divide_ByZero_ThrowsException()
        => Assert.Throws<DivideByZeroException>(() => _sut.Divide(10, 0));

    [Xunit.Theory]
    [InlineData(-5, -3, -8)]
    [InlineData(0, 0, 0)]
    [InlineData(1.5, 2.5, 4)]
    public void Add_VariousInputs_ReturnsExpectedSum(double a, double b, double expected)
        => Assert.Equal(expected, _sut.Add(a, b));
}