## List of Methods

### PolymorphicQuery&lt;T&gt;(sql [, param] [, discriminator])

Do polymorphic query and return a collection of base type.

> **sql**: string<br />
> **param**: object (Default is null)<br />
> **discriminator**: string (Default is "Discriminator")

### PolymorphicQuerySingle&lt;T&gt;(sql [, param] [, discriminator])

Do polymorphic query and return only one of base type.

> **sql**: string<br />
> **param**: object (Default is null)<br />
> **discriminator**: string (Default is "Discriminator")

## Custom RowParser

Default is getting row parser by finding derive type with matching discriminator value precisely. We can implement *`IRowParserProvider`* and assign to `Chef.Extensions.Dapper.Extension.RowParserProvider` change default row parser.