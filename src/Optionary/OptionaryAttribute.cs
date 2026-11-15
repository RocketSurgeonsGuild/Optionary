using System.Diagnostics;

namespace Optionary;

/// <summary>
///     Declares the configuration section of an options class. Optionary's source generator describes the class as JSON Schema,
///     which is merged into the application's <c>appsettings.schema.json</c> for editor completion and validation, and registers
///     it with the options pipeline.
/// </summary>
/// <remarks>
///     <para>
///         Without a section path the section is the type name with an <c>Options</c>, <c>Settings</c>, <c>Configuration</c>
///         or <c>Config</c> suffix removed, so <c>GreetingsOptions</c> becomes <c>Greetings</c>. Nested sections use
///         <c>:</c>, as in <c>"Http:Retry"</c>.
///     </para>
///     <para>
///         A type has one section, so it can be declared once. For a type you don't own, use the assembly form,
///         <see cref="OptionaryAttribute{T}" />.
///     </para>
///     <para>
///         <see cref="KeyStyle" /> picks how the keys of the type and of the types nested in it are spelled (as declared, Pascal,
///         camel, dash or underscore case). A type that several sections reach has to spell its keys the same way in all of them;
///         OPT0011 reports a mismatch.
///     </para>
///     <para>
///         The generated registration reads its own section from the application's complete <c>IConfiguration</c> and creates or
///         binds the options.
///     </para>
///     <para>Only works with source generators enabled.</para>
/// </remarks>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class)]
[Conditional("CodeGeneration")]
public sealed class OptionaryAttribute : Attribute
{
    /// <summary>
    ///     Declares the attributed class under the section derived from its name.
    /// </summary>
    public OptionaryAttribute() { }

    /// <summary>
    ///     Declares the attributed class under <paramref name="section" />.
    /// </summary>
    /// <param name="section">The section path, e.g. <c>"Greetings"</c> or <c>"Http:Retry"</c>.</param>
    public OptionaryAttribute(string section) => Section = section;

    /// <summary>
    ///     The section path, or <see langword="null" /> for the one derived from the type name.
    /// </summary>
    public string? Section { get; }

    /// <summary>
    ///     Whether to register <c>IOptions&lt;T&gt;</c> for the type. <see langword="false" /> only describes the section in the
    ///     schema. The default is <see langword="true" />.
    /// </summary>
    public bool RegisterOptions { get; set; } = true;

    /// <summary>
    ///     Whether to validate the options when the host starts. The default is <see langword="false" />.
    /// </summary>
    public bool ValidateOnStart { get; set; }

    /// <summary>
    ///     How the configuration keys are spelled: those of every bound property and constructor parameter of the options type and
    ///     of the object types nested in it, and the default section path derived from the type name. The default is
    ///     <see cref="ConfigurationKeyStyle.Undefined" />, which keeps the declared names. See <see cref="ConfigurationKeyStyle" />.
    /// </summary>
    public ConfigurationKeyStyle KeyStyle { get; set; }
}

/// <summary>
///     Declares the configuration section of an options type <typeparamref name="T" /> from the assembly, for types you don't
///     own.
/// </summary>
/// <remarks>
///     <para>
///         For example, <c>[assembly: Optionary&lt;HostOptions&gt;("Hosting")]</c>.
///     </para>
///     <para>
///         The section path follows the same rules as <see cref="OptionaryAttribute" />. Apply it once for each type you
///         configure. A type has one section: naming it twice, or also declaring it with the class form, is reported as OPT0010.
///         <see cref="KeyStyle" /> works as it does on the class form.
///     </para>
///     <para>Only works with source generators enabled.</para>
/// </remarks>
/// <typeparam name="T">The options type.</typeparam>
[PublicAPI]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[Conditional("CodeGeneration")]
public sealed class OptionaryAttribute<T> : Attribute
    where T : class
{
    /// <summary>
    ///     Declares <typeparamref name="T" /> under the section derived from its name.
    /// </summary>
    public OptionaryAttribute() { }

    /// <summary>
    ///     Declares <typeparamref name="T" /> under <paramref name="section" />.
    /// </summary>
    /// <param name="section">The section path, e.g. <c>"Greetings"</c> or <c>"Http:Retry"</c>.</param>
    public OptionaryAttribute(string section) => Section = section;

    /// <summary>
    ///     The section path, or <see langword="null" /> for the one derived from the type name.
    /// </summary>
    public string? Section { get; }

    /// <summary>
    ///     Whether to register <c>IOptions&lt;T&gt;</c> for the type. <see langword="false" /> only describes the section in the
    ///     schema. The default is <see langword="true" />.
    /// </summary>
    public bool RegisterOptions { get; set; } = true;

    /// <summary>
    ///     Whether to validate the options when the host starts. The default is <see langword="false" />.
    /// </summary>
    public bool ValidateOnStart { get; set; }

    /// <summary>
    ///     How the configuration keys are spelled: those of every bound property and constructor parameter of the options type and
    ///     of the object types nested in it, and the default section path derived from the type name. The default is
    ///     <see cref="ConfigurationKeyStyle.Undefined" />, which keeps the declared names. See <see cref="ConfigurationKeyStyle" />.
    /// </summary>
    public ConfigurationKeyStyle KeyStyle { get; set; }
}
