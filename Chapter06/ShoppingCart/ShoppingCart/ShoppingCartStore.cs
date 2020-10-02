namespace ShoppingCart.ShoppingCart
{
  using System.Data;
  using System.Data.SqlClient;
  using System.Linq;
  using System.Threading.Tasks;
  using Dapper;

  public interface IShoppingCartStore
  {
    Task<ShoppingCart>Get(int userId);
    Task Save(ShoppingCart shoppingCart);
  }

  public class ShoppingCartStore : IShoppingCartStore
  {
    private string connectionString =
      @"Data Source=localhost;Initial Catalog=ShoppingCart;
User Id=SA; Password=yourStrong(!)Password";

    private const string readItemsSql =
      @"
select ShoppingCart.ID, ProductCatalogId, ProductName, ProductDescription, Currency, Amount 
from ShoppingCart, ShoppingCartItem
where ShoppingCartItem.ShoppingCartId = ShoppingCart.ID
and ShoppingCart.UserId=@UserId";

    public async Task<ShoppingCart> Get(int userId)
    {
      await using var conn = new SqlConnection(this.connectionString);
      var items = (await
          conn.QueryAsync(
            readItemsSql,
            new {UserId = userId}))
        .ToList();
      return new ShoppingCart(
        items.FirstOrDefault()?.ID,
        userId,
        items.Select(x =>
          new ShoppingCartItem(
            (int) x.ProductCatalogId,
            x.ProductName,
            x.ProductDescription,
            new Money(x.Currency, x.Amount))));
    }

    private const string insertShoppingCartSql =
@"insert into ShoppingCart (UserId) OUTPUT inserted.ID VALUES (@UserId)";

    private const string deleteAllForShoppingCartSql =
@"delete item from ShoppingCartItem item
inner join ShoppingCart cart on item.ShoppingCartId = cart.ID
and cart.UserId=@UserId";

    private const string addAllForShoppingCartSql =
@"insert into ShoppingCartItem
(ShoppingCartId, ProductCatalogId, ProductName,
ProductDescription, Amount, Currency)
values
(@ShoppingCartId, @ProductCatalogId, @ProductName,
@ProductDescription, @Amount, @Currency)";

    public async Task Save(ShoppingCart shoppingCart)
    {
      await using var conn = new SqlConnection(this.connectionString);
      await conn.OpenAsync();
      await using (var tx = conn.BeginTransaction())
      {
        var shoppingCartId =
          shoppingCart.Id ?? await conn.QuerySingleAsync<int>(insertShoppingCartSql, new {shoppingCart.UserId}, tx);

        await conn.ExecuteAsync(
          deleteAllForShoppingCartSql,
          new {UserId = shoppingCart.UserId},
          tx);
        await conn.ExecuteAsync(
          addAllForShoppingCartSql,
          shoppingCart.Items.Select(x =>
            new
            {
              shoppingCartId,
              x.ProductCatalogId,
              Productdescription = x.Description,
              x.ProductName,
              x.Price.Amount,
              x.Price.Currency
            }),
          tx);
      }
    }
  }
}