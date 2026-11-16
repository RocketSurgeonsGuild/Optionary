using Microsoft.Extensions.Options;

namespace Optionary.Samples.Library;

/// <summary>
///     Enforces <see cref="GreetingsOptions" />'s data annotations at run time. <c>[OptionsValidator]</c> is the options
///     validation source generator from Microsoft.Extensions.Options; the generated registration method registers it.
/// </summary>
[OptionsValidator]
public sealed partial class GreetingsOptionsValidator : IValidateOptions<GreetingsOptions>;
