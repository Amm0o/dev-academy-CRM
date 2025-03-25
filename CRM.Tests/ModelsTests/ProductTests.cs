using CRM.Models;
using Xunit;


namespace CRM.Tests.ModelsTests;

public class ProductTests {

        string productName= "test";
        string productDescription="Test description";
        string prodcutCategory="N/A";
        double productPrice = 2.7;
        int productQuantity= 7;

    [Fact]
    public void Constructor_ValidData_InitializesCorrectly() {
        var product = new Product(productName, productDescription, productPrice, productQuantity, prodcutCategory);

        Assert.Equal(productName, product.ProductName);
        Assert.Equal(productDescription, product.ProductDescription);
        Assert.Equal(productPrice, product.ProductPrice);
        Assert.Equal(productQuantity, product.ProductQuantity);
        Assert.Equal(prodcutCategory, product.ProdcutCategory);
    }
}