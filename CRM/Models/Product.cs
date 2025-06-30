using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Data;

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
    public DateTime ProductCreateTime { get; private set; }
    public Guid ProductGuid { get; private set; }
    

    [SetsRequiredMembers]
    public Product(string productName, string productDescription, double productPrice, int productQuantity, string productCategory, Guid? productGuid = null)
    {
        ProductName = productName;
        ProductDescription = productDescription;
        ProductPrice = productPrice;
        ProductQuantity = productQuantity;
        ProductCategory = productCategory;
        _productUpdateTime = DateTime.UtcNow;
        ProductGuid = productGuid ?? Guid.NewGuid(); // If no guid passed in create one
    }


    // Factory method for creating new products
    public static Product CreateNew(string name, string description, string category, double price, int stock)
    {
        return new Product(
            name,
            description,
            price,
            stock,
            category
        );
    }

    // Create from database data
    public static Product FromDatabase(DataRow row)
    {
        return new Product(
            row["Name"].ToString(),
            row["Description"].ToString(),
            Convert.ToDouble(row["Price"]),
            Convert.ToInt32(row["Stock"]),
            row["Category"].ToString(),
            Guid.Parse(row["ProductGuid"].ToString())
        )
        {
            Id = Convert.ToInt32(row["ProductId"]),
            ProductCreateTime = Convert.ToDateTime(row["CreatedAt"]),
            _productUpdateTime = Convert.ToDateTime(row["UpdatedAt"])
        };
    }

    // Convert to anonymous object for API response
    public object ToApiResponse()
    {
        return new
        {
            ProductId = Id,
            ProductName = ProductName,
            ProductDescription = ProductDescription,
            ProductCategory = ProductCategory,
            ProductPrice = ProductPrice,
            ProductStock = ProductQuantity,
            ProductGuid = ProductGuid,
            CreatedAt = ProductCreateTime,
            UpdatedAt = ProductUpdateTime
        };
    }

    // Update product details 
    public void UpdateDetails (string name, string description, string category, double price, int stock)
    {
        ProductName = name;
        ProductDescription = description;
        ProductCategory = category;
        ProductPrice = price;
        ProductQuantity = stock;
        _productUpdateTime = DateTime.UtcNow;
    }

    public static List<string> getCategories(string categories)
    {
        // If categories is empty return empty list
        if (string.IsNullOrWhiteSpace(categories))
            return new List<string>();

        return categories.Split(",").Select(cat => cat.Trim()).Where(cat => !string.IsNullOrWhiteSpace(cat)).ToList();
    }
}