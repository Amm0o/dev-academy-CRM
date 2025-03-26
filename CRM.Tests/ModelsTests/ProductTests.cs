using CRM.Models;
using Xunit;


namespace CRM.Tests.ModelsTests;

public class ProductTests {

        string productName= "test";
        string productDescription="Test description";
        string productCategory="N/A";
        double productPrice = 2.7;
        int productQuantity= 7;

    [Fact]
    public void Constructor_ValidData_InitializesCorrectly() {
        var product = new Product(productName, productDescription, productPrice, productQuantity, productCategory);

        Assert.Equal(productName, product.ProductName);
        Assert.Equal(productDescription, product.ProductDescription);
        Assert.Equal(productPrice, product.ProductPrice);
        Assert.Equal(productQuantity, product.ProductQuantity);
        Assert.Equal(productCategory, product.ProductCategory);
    }

    [Theory]
    [InlineData("", "Valid description", 2.7, 7, "Valid category")]
    [InlineData("Valid name", "", 2.7, 7, "Valid category")]
    [InlineData("Valid name", "Valid description", -1.0, 7, "Valid category")]
    [InlineData("Valid name", "Valid description", 2.7, -5, "Valid category")]
    [InlineData("Valid name", "Valid description", 2.7, 7, "")]
    public void Constructor_InvalidData_ThrowsArgumentException(string name, string description, double price, int quantity, string category)
    {
        Assert.Throws<ArgumentException>(() => new Product(name, description, price, quantity, category));
    }

    [Fact]
    public void SetProductPrice_NegativeValue_ThrowsArgumentException()
    {
        var product = new Product(productName, productDescription, productPrice, productQuantity, productCategory);
        Assert.Throws<ArgumentException>(() => product.ProductPrice = -10);
    }

    [Fact]
    public void SetProductQuantity_NegativeValue_ThrowsArgumentException()
    {
        var product = new Product(productName, productDescription, productPrice, productQuantity, productCategory);
        Assert.Throws<ArgumentException>(() => product.ProductQuantity = -3);
    }
}