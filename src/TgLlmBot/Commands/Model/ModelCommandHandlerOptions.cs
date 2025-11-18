using System;

namespace TgLlmBot.Commands.Model;

public class ModelCommandHandlerOptions
{
    public ModelCommandHandlerOptions(Uri endpoint, string model)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(model));
        }

        Endpoint = endpoint;
        Model = model;
    }

    public Uri Endpoint { get; }
    public string Model { get; }
}
