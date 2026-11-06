namespace PC212242.UPractica.Clerky.Platform.Shared.Domain.Model.ValueObjects;

/// <summary>
/// Represents a monetary amount and its currency.
/// </summary>
/// <remarks>
/// Author: Andrea Correa Rodríguez.
/// </remarks>
public record MonetaryAmount
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public MonetaryAmount(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Monetary amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentNullException(
                nameof(currency),
                "Currency cannot be empty.");

        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Multiplies the monetary amount by a factor.
    /// </summary>
    /// <param name="factor">The multiplication factor.</param>
    /// <returns>A new monetary amount with the same currency.</returns>
    public MonetaryAmount Multiply(decimal factor)
    {
        return new MonetaryAmount(Amount * factor, Currency);
    }
}