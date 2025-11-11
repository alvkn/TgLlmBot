using System;

namespace TgLlmBot.Commands.Model;

public class ModelCommandHandlerOptions
{
    public ModelCommandHandlerOptions(string endpoint, string model)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(endpoint));
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(model));
        }

        Endpoint = endpoint;
        Model = model;
    }

    public string Endpoint { get; }
    public string Model { get; }
}
