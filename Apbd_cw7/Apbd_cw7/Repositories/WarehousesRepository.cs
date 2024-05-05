using System.Data;
using System.Data.Common;
using Apbd_cw7.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Apbd_cw7.Repositories;

public class WarehousesRepository : IWarehousesRepository
{
    private readonly IConfiguration _configuration;

    public WarehousesRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> DoesProductExists(int id)
    {
        var query = "SELECT 1 FROM Product WHERE IdProduct = @Id";

        using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = query;
        command.Parameters.AddWithValue("@Id", id);

        await connection.OpenAsync();

        var result = await command.ExecuteScalarAsync();

        return result is not null;
    }

    public async Task<bool> DoesWarehouseExists(int id)
    {
        var query = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @Id";

        using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = query;
        command.Parameters.AddWithValue("@Id", id);

        await connection.OpenAsync();

        var result = await command.ExecuteScalarAsync();

        return result is not null;
    }

    public async Task<bool> DoesOrderExists(int idProduct, int amount, DateTime createdAt)
    {
        var query = "SELECT 1 FROM [Order] " +
                    "WHERE IdProduct = @ID AND " +
                    "Amount = @Amount AND " +
                    "FulfilledAt IS NULL AND " +
                    "CreatedAt < @CreatedAt";
        using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = query;
        
        command.Parameters.AddWithValue("@ID", idProduct);
        command.Parameters.AddWithValue("@Amount", amount);
        command.Parameters.AddWithValue("@CreatedAt", createdAt);

        await connection.OpenAsync();

        var result = await command.ExecuteScalarAsync();

        return result is not null;
    }

    public async Task<int> GetOrderId(int id, int amount)
    {
        var query = "SELECT IdOrder FROM [Order] WHERE IdProduct = @IdProduct AND Amount = @Amount";

        using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = query;

        command.Parameters.AddWithValue("@IdProduct", id);
        command.Parameters.AddWithValue("@Amount", amount);

        await connection.OpenAsync();

        var reader = await command.ExecuteReaderAsync();
        
        int IdOrderOrdinal = reader.GetOrdinal("IdOrder");

        Task<int>? result = null;
        
        while (reader.Read())
            result = Task.FromResult(reader.GetInt32(IdOrderOrdinal));

        return await result;
    }

    public async Task<double> GetProductPrice(int idProduct)
    {
        var query = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";

        using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = query;

        command.Parameters.AddWithValue("@IdProduct", idProduct);

        await connection.OpenAsync();

        var reader = await command.ExecuteReaderAsync();

        int PriceOrdinal = reader.GetOrdinal("Price");

        Task<decimal> result = null;

        while (reader.Read())
            result = Task.FromResult(reader.GetDecimal(PriceOrdinal));
        
        return (double)await result;
    }
    
    public async Task RefillProducts(WarehouseProductDTO warehouseProductDto)
    {
        var updateQuery = "UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdProduct = @IdProduct AND Amount = @Amount";
        var insertQuery = "INSERT INTO Product_Warehouse VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, GETDATE())";

        using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        await connection.OpenAsync();
        
        int IdOrder = GetOrderId(warehouseProductDto.IdProduct, warehouseProductDto.Amount).Result;
        double ProductPrice = GetProductPrice(warehouseProductDto.IdProduct).Result;
        double ResultPrice = ProductPrice * warehouseProductDto.Amount;

        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = (SqlTransaction)transaction;

        try
        {
            command.Parameters.Clear();
            command.CommandText = updateQuery;
            
            command.Parameters.AddWithValue("@IdProduct", warehouseProductDto.IdProduct);
            command.Parameters.AddWithValue("@Amount", warehouseProductDto.Amount);
        
            await command.ExecuteNonQueryAsync();
            
            command.Parameters.Clear();
            command.CommandText = insertQuery;
            
            command.Parameters.AddWithValue("@IdWarehouse", warehouseProductDto.IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", warehouseProductDto.IdProduct);
            command.Parameters.AddWithValue("@IdOrder", IdOrder);
            command.Parameters.AddWithValue("@Amount", warehouseProductDto.Amount);
            command.Parameters.AddWithValue("@Price",ResultPrice);

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            Console.WriteLine(e);
            throw;
        }
        
    }

    public async Task RefillProductsWithProcedure(WarehouseProductDTO warehouseProductDto)
    {
        var query = "CREATE PROCEDURE AddProductToWarehouse @IdProduct INT, @IdWarehouse INT, @Amount INT,  \n@CreatedAt DATETIME\nAS  \nBEGIN  \n   \n DECLARE @IdProductFromDb INT, @IdOrder INT, @Price DECIMAL(5,2);  \n  \n SELECT TOP 1 @IdOrder = o.IdOrder  FROM \"Order\" o   \n LEFT JOIN Product_Warehouse pw ON o.IdOrder=pw.IdOrder  \n WHERE o.IdProduct=@IdProduct AND o.Amount=@Amount AND pw.IdProductWarehouse IS NULL AND  \n o.CreatedAt<@CreatedAt;  \n  \n SELECT @IdProductFromDb=Product.IdProduct, @Price=Product.Price FROM Product WHERE IdProduct=@IdProduct  \n   \n IF @IdProductFromDb IS NULL  \n BEGIN  \n  RAISERROR('Invalid parameter: Provided IdProduct does not exist', 18, 0);  \n  RETURN;  \n END;  \n  \n IF @IdOrder IS NULL  \n BEGIN  \n  RAISERROR('Invalid parameter: There is no order to fullfill', 18, 0);  \n  RETURN;  \n END;  \n   \n IF NOT EXISTS(SELECT 1 FROM Warehouse WHERE IdWarehouse=@IdWarehouse)  \n BEGIN  \n  RAISERROR('Invalid parameter: Provided IdWarehouse does not exist', 18, 0);  \n  RETURN;  \n END;  \n  \n SET XACT_ABORT ON;  \n BEGIN TRAN;  \n   \n UPDATE \"Order\" SET  \n FulfilledAt=@CreatedAt  \n WHERE IdOrder=@IdOrder;  \n  \n INSERT INTO Product_Warehouse(IdWarehouse,   \n IdProduct, IdOrder, Amount, Price, CreatedAt)  \n VALUES(@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Amount*@Price, @CreatedAt);  \n   \n SELECT @@IDENTITY AS NewId;\n   \n COMMIT;  \nEND";

        using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        
        using SqlCommand command = new SqlCommand("AddProductToWarehouse", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        
        command.Parameters.AddWithValue("@IdProduct", warehouseProductDto.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", warehouseProductDto.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", warehouseProductDto.Amount);
        command.Parameters.AddWithValue("@CreatedAt",warehouseProductDto.CreatedAt);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public bool CheckAmount(int amount)
    {
        return amount > 0;
    }
}