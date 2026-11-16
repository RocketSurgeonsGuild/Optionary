# Glossary

**Options type**: a class or record whose instance holds one configuration section's values, read by code through `IOptions<T>`.

**Section**: the part of the application's configuration an options type is read from, named by its **section path** (for example `Http:Retry`). An options type has exactly one section.

**Declaration**: marking an options type with its section, either on the type itself (`[Optionary]`) or, for a type you don't own, on the assembly (`[assembly: Optionary<T>]`). A rejected declaration is reported and contributes nothing.

**Key style**: how a declaration spells the keys of its options type and everything nested in it (as declared, Pascal, camel, dash or underscore case). An explicit key name on a member always wins.

**Registration method**: the method generated into each assembly that has declarations, which registers that assembly's options types with the options pipeline. It exists even when every declaration was rejected.

**Fragment**: the description of one section's shape that an assembly carries with it, so that projects referencing the assembly can learn the section without running its code.

**Fragment item**: an `OptionarySchemaFragment` MSBuild item, naming a section path and holding its fragment inline or in a JSON file. It becomes the assembly's fragment for that section, so a package's build files can declare a section without code.

**Library schema**: the file a library packs, describing only its own sections. It is permissive.

**Host**: a project whose build combines its own fragments with those of everything it references: applications and test projects. Not to be confused with Dovetail's host model (web, MAUI, Uno, ...).

**Application schema**: the one file a host's build writes, covering every section it can see plus the common appsettings keys. It is strict: unknown keys in a section are flagged, ignoring case.

**Merge**: producing the application schema from fragments. The same fragments always give the same file, whatever order the references come in.
