## Polymorphic Query

Parse data what query from database to polymorphism.

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

### PolymorphicQuerySingleOrDefault&lt;T&gt;(sql [, param] [, discriminator])

Do polymorphic query and return only one or default value of base type.

> **sql**: string<br />
> **param**: object (Default is null)<br />
> **discriminator**: string (Default is "Discriminator")

### PolymorphicQueryFirst&lt;T&gt;(sql [, param] [, discriminator])

Do polymorphic query and return first of base type.

> **sql**: string<br />
> **param**: object (Default is null)<br />
> **discriminator**: string (Default is "Discriminator")

### PolymorphicQueryFirstOrDefault&lt;T&gt;(sql [, param] [, discriminator])

Do polymorphic query and return first or default value of base type.

> **sql**: string<br />
> **param**: object (Default is null)<br />
> **discriminator**: string (Default is "Discriminator")

### PolymorphicInsert(sql, param)

Do polymorphic inserting.

> **sql**: string<br />
> **param**: object

### HierarchyInsert(sql, param)

Do hierarchy inserting.

> **sql**: string<br />
> **param**: object

## Examples

Suppose the `Food` table is:

![](https://i.imgur.com/Mw6EErT.png)

Base class `Food` and derived classes:

```cs
public abstract class Food
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Discriminator => this.GetType().Name;
}

public class Dessert : Food
{
    public int Calorie { get; set; }
}

public class DryGoods : Food
{
    public string CountryOfOrigin { get; set; }
}

public class Delicatessen : Food
{
    public string Chef { get; set; }
}
```

### Query foods that Id is 1, 2, 4 and return polymorphic results.

```cs
using (var db = new SqlConnection(connectionString))
{
    var sql = @"
SELECT
    f.Id
   ,f.[Name]
   ,f.Discriminator
   ,f.Calorie
   ,f.CountryOfOrigin
   ,f.Chef
FROM Food f
WHERE f.Id IN (1, 2, 4)";

    var results = db.PolymorphicQuery<Food>(sql);
}
```

The executed results:

```json
[
  {
    "$type": "LabForm462.Model.Data.Dessert, LabForm462",
    "Calorie": 300,
    "Id": 1,
    "Name": "蛋糕",
    "Discriminator": "Dessert"
  },
  {
    "$type": "LabForm462.Model.Data.DryGoods, LabForm462",
    "CountryOfOrigin": "台灣",
    "Id": 2,
    "Name": "乾香菇",
    "Discriminator": "DryGoods"
  },
  {
    "$type": "LabForm462.Model.Data.Delicatessen, LabForm462",
    "Chef": "Johnny",
    "Id": 4,
    "Name": "涼拌毛豆",
    "Discriminator": "Delicatessen"
  }
]
```

### Insert polymorphic collection to database.

```cs
var foods = new List<Food>
            {
                new Dessert
                {
                    Name = "Cake111",
                    Calorie = 100,
                    ShelfLife = new ShelfLife { Months = 0, Days = 3 }
                },
                new DryGoods
                {
                    Name = "Shiitake222",
                    CountryOfOrigin = "Taiwan",
                    ShelfLife = new ShelfLife { Months = 12, Days = 0 }
                },
                new Delicatessen
                {
                    Name = "Bun333",
                    Chef = "Mary",
                    ShelfLife = new ShelfLife { Months = 0, Days = 3 }
                }
            };

using (var db = new SqlConnection(connectionString))
{
    var sql = @"
INSERT INTO Food([Name]
                ,ShelfLife_Months
                ,ShelfLife_Days
                ,Discriminator
                ,Calorie
                ,CountryOfOrigin
                ,Chef)
    VALUES (@Name
           ,@ShelfLife_Months
           ,@ShelfLife_Days
           ,@Discriminator
           ,@Calorie
           ,@CountryOfOrigin
           ,@Chef);";

    db.PolymorphicInsert(sql, foods);
}
```

Split hierarchical properties by underscore as `ShelfLife` in example. The executed result is:

![](https://i.imgur.com/g5tCZJ3.png)

## Custom RowParser

Default is getting row parser by finding derived type with matching discriminator value precisely. We can implement *`IRowParserProvider`* and assign to `Chef.Extensions.Dapper.Extension.RowParserProvider` to change default row parser.

## Query as Immutability

Dapper maps data to immutable class, and the constructor fitting parameters count and sequence must be matched. There are some immutable query extension methods that do not need fitting parameters count and sequence.

    public static IEnumerable<T> QueryAsImmutability<T>(this IDbConnection cnn, string sql, object param = null);
    public static T QueryFirstAsImmutability<T>(this IDbConnection cnn, string sql, object param = null);
    public static T QueryFirstOrDefaultAsImmutability<T>(this IDbConnection cnn, string sql, object param = null);
    public static T QuerySingleAsImmutability<T>(this IDbConnection cnn, string sql, object param = null);
    public static T QuerySingleOrDefaultAsImmutability<T>(this IDbConnection cnn, string sql, object param = null);

## Generate universal SQL statements

### ToSearchCondition&lt;T&gt;([alias], out IDictionary<string, object> parameters)

Generate search condition. Parameter must be placed left of operator as example. Support `Arrary.Contains()` method by array variable or array initializer. And we can use `Column` and `StringLength` annotations to tag column name, char type and char length.

> **alias**: string (Default is null)<br />
> **out parameters**: IDictionary<string, object>, parameters of statement.

Example:

    public void Test_ToSearchCondition()
    {
        var arr = new[] { "1", "2" };
        
        Expression<Func<Member, bool>> predicate = x => x.Id < 1 && arr.Contains(x.FirstName);

        var searchCondition = predicate.ToSearchCondition(out var parameters);
        
        // searchCondition is "([Id] < {=Id_0}) AND ([first_name] = @FirstName_0 OR [first_name] = @FirstName_1)".
    }
    
    internal class Member
    {
        public int Id { get; set; }

        [Column("first_name", TypeName = "varchar")]
        [StringLength(20)]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }
    }

### ToSearchCondition&lt;T&gt;([alias], IDictionary<string, object> parameters)

Difference ToSearchCondition&lt;T&gt;([alias], out IDictionary<string, object> parameters) method is that will add parameters in `parameters` for avoiding generate same parameter name.

### ToSelectList&lt;T&gt;([alias])

Generate select list.

> **alias**: string (Default is null)

Example:

    public void Test_ToSelectList()
    {
        Expression<Func<Member, object>> select = x => new { x.Id, x.FirstName, x.LastName };

        var selectList = select.ToSelectList();

        // selectList is "[Id], [first_name] AS [FirstName], [last_name] AS [LastName]".
    }
    
    internal class Member
    {
        public int Id { get; set; }

        [Column("first_name", TypeName = "varchar")]
        [StringLength(20)]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }
    }

### ToSetStatements&lt;T&gt;([alias], out IDictionary<string, object> parameters)

Generate set statements. That must be object initializer as example. Assigned value must be variable or constant.

> **alias**: string (Default is null)<br />
> **out parameters**: IDictionary<string, object>, parameters of statement.

Example:

    public void Test_ToSetStatements()
    {
        Expression<Func<Member>> setters = () => new Member { FirstName = "abab", LastName = "baba" };

        var setStatements = setters.ToSetStatements(out var parameters);

        // setStatements is "[first_name] = @FirstName_0, [last_name] = @LastName_0".
    }
    
    internal class Member
    {
        public int Id { get; set; }

        [Column("first_name", TypeName = "varchar")]
        [StringLength(20)]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }
    }

### ToColumnList&lt;T&gt;(out string valueList, out IDictionary<string, object> parameters)

Generate column list and value list for insertioin. That must be object initializer as example. Assigned value must be variable or constant.

> **out valueList**: string.<br />
> **out parameters**: IDictionary<string, object>, parameters of statement.

Example:

    public void Test_ToColumnList()
    {
        Expression<Func<Member>> setters = () => new Member { Id = 123, FirstName = "abab", LastName = "baba" };

        var columnList = setters.ToColumnList(out var valueList, out var parameters);

        // columnList is "[Id], [first_name], [last_name]".
        // valueList is "{=Id_0}, @FirstName_0, @LastName_0".
    }
    
    [Table("user")]
    internal class Member
    {
        public int Id { get; set; }

        [Column("first_name", TypeName = "varchar")]
        [StringLength(20)]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }
    }

### ToOrderAscending&lt;T&gt;()

Generate ascending order expression.

Example:

    public void Test_ToAscendingOrder()
    {
        Expression<Func<Member, object>> orderBy = x => x.FirstName;

        var orderExpression = orderBy.ToOrderAscending("m");

        // orderExpression is "[first_name] ASC".
    }
    
    [Table("user")]
    internal class Member
    {
        public int Id { get; set; }

        [Column("first_name", TypeName = "varchar")]
        [StringLength(20)]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }
    }

### ToOrderDescending&lt;T&gt;()

Generate descending order expression.

Example:

    public void Test_ToDescendingOrder()
    {
        Expression<Func<Member, object>> orderBy = x => x.FirstName;

        var orderExpression = orderBy.ToOrderDescending("m");

        // orderExpression is "[first_name] DESC".
    }
    
    [Table("user")]
    internal class Member
    {
        public int Id { get; set; }

        [Column("first_name", TypeName = "varchar")]
        [StringLength(20)]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }
    }

