using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CRM.Models;

public sealed class Product : Entity
{
    private string _productName;
    private string _productDescription;
    private double _productPrice;
    private int _productQuantity;
    private string _productCategory;
    private DateTime _productUpdateTime;

    [Required(ErrorMessage = "Name of the product is required")]
    [StringLength(100, MinimumLength = 2)]
    public string ProductName
    {
        get => _productName;
        private set
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
                throw new ArgumentException("Name of product is required and needs to be at least 2 characters.");
            _productName = value;
        }
    }

    [Required(ErrorMessage = "A product needs a description")]
    [StringLength(300)]
    public string ProductDescription
    {
        get => _productDescription;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A product needs a description.");
            _productDescription = value;
        }
    }

    [Required(ErrorMessage = "A product needs a price!")]
    [Range(0, double.MaxValue)]
    public double ProductPrice
    {
        get => _productPrice;
        set
        {
            if (value < 0)
                throw new ArgumentException("Product price must be >= 0.");
            _productPrice = value;
        }
    }

    [Required(ErrorMessage = "The product needs to have a quantity that's >= 0")]
    [Range(0, int.MaxValue)]
    public int ProductQuantity
    {
        get => _productQuantity;
        set
        {
            if (value < 0)
                throw new ArgumentException("The product quantity must be >= 0.");
            _productQuantity = value;
        }
    }

    [Required(ErrorMessage = "Product needs to have a category at least N/A")]
    [StringLength(50, MinimumLength = 3)]
    public string ProductCategory
    {
        get => _productCategory;
        private set
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 3)
                throw new ArgumentException("Product needs to have a category at least 3 characters.");
            _productCategory = value;
        }
    }

    public DateTime ProductUpdateTime => _productUpdateTime;
    public Guid ProductGuid { get; private set; }

    [SetsRequiredMembers]
    public Product(string productName, string productDescription, double productPrice, int productQuantity, string productCategory, Guid? productGiud = null)
    {
        ProductName = productName;
        ProductDescription = productDescription;
        ProductPrice = productPrice;
        ProductQuantity = productQuantity;
        ProductCategory = productCategory;
        _productUpdateTime = DateTime.UtcNow;
        ProductGuid = Guid.NewGuid();
    }
    

    public static Product CreateNew(string name, string description, string category, double price, int stock)
    {
        return new Product(
            name,
            description,
            price,
            stock,
            category,
            Guid.NewGuid()
        );
    }
}