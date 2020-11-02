# Introduction
Dear Postgres team,

This is a minimal sample repo for a potential bug ([issue 1554](https://github.com/npgsql/efcore.pg/issues/1554)), and thus a repo for you. Thank you very much for taking your time to check this issue.

# Scenario / Use Case
We use columns with arrays in our database, for example to store topics. Every user can enter these topics as he likes, so we intentionally decided against a dedicated table for the topics.

Now the users should be able to search this data, even partially. Let us assume that a user has entered the topics `["Artificial Intelligence", "Big Data", "Robotics"]`. Then the search for `"intelli"` should also find this entry.

# Issue
In the `Program.cs` you find four attempts to address the use case:

- 1st attempt, starting from [line 59](https://github.com/SommerEngineering/BugILikePostgres/blob/main/BugILikePostgres/Program.cs#L59): Regarding to your ["Array Type Mapping" documentation](https://www.npgsql.org/efcore/mapping/array.html), this seems to be the way to go. Using `Where(n => n.Topics.Any(s => EF.Functions.ILike(s, $"%{searchTerm1}%")))`. Unfortunately, this throws an `NullReferenceException`. Stacktrace:

```
System.NullReferenceException: Object reference not set to an instance of an object.
   at Npgsql.EntityFrameworkCore.PostgreSQL.Query.NpgsqlSqlExpressionFactory.ApplyTypeMappingsOnItemAndArray(SqlExpression itemExpression, SqlExpression arrayExpression)
   at Npgsql.EntityFrameworkCore.PostgreSQL.Query.NpgsqlSqlExpressionFactory.ApplyTypeMappingOnAny(PostgresAnyExpression postgresAnyExpression)
   at Npgsql.EntityFrameworkCore.PostgreSQL.Query.NpgsqlSqlExpressionFactory.ApplyTypeMapping(SqlExpression sqlExpression, RelationalTypeMapping typeMapping)
   at Microsoft.EntityFrameworkCore.Query.SqlExpressionFactory.ApplyDefaultTypeMapping(SqlExpression sqlExpression)
   at Npgsql.EntityFrameworkCore.PostgreSQL.Query.NpgsqlSqlExpressionFactory.Any(SqlExpression item, SqlExpression array, PostgresAnyOperatorType operatorType)
   at Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal.NpgsqlSqlTranslatingExpressionVisitor.VisitArrayMethodCall(MethodInfo method, ReadOnlyCollection`1 arguments)
   at Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal.NpgsqlSqlTranslatingExpressionVisitor.VisitMethodCall(MethodCallExpression methodCall)
   at System.Linq.Expressions.MethodCallExpression.Accept(ExpressionVisitor visitor)
   at System.Linq.Expressions.ExpressionVisitor.Visit(Expression node)
   at Microsoft.EntityFrameworkCore.Query.RelationalSqlTranslatingExpressionVisitor.TranslateInternal(Expression expression)
   at Microsoft.EntityFrameworkCore.Query.RelationalSqlTranslatingExpressionVisitor.Translate(Expression expression)
   at Microsoft.EntityFrameworkCore.Query.RelationalQueryableMethodTranslatingExpressionVisitor.TranslateExpression(Expression expression)
   at Microsoft.EntityFrameworkCore.Query.RelationalQueryableMethodTranslatingExpressionVisitor.TranslateLambdaExpression(ShapedQueryExpression shapedQueryExpression, LambdaExpressi
on lambdaExpression)
   at Microsoft.EntityFrameworkCore.Query.RelationalQueryableMethodTranslatingExpressionVisitor.TranslateWhere(ShapedQueryExpression source, LambdaExpression predicate)
   at Microsoft.EntityFrameworkCore.Query.QueryableMethodTranslatingExpressionVisitor.VisitMethodCall(MethodCallExpression methodCallExpression)
   at System.Linq.Expressions.MethodCallExpression.Accept(ExpressionVisitor visitor)
   at System.Linq.Expressions.ExpressionVisitor.Visit(Expression node)
   at Microsoft.EntityFrameworkCore.Query.QueryCompilationContext.CreateQueryExecutor[TResult](Expression query)
   at Microsoft.EntityFrameworkCore.Storage.Database.CompileQuery[TResult](Expression query, Boolean async)
   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.CompileQueryCore[TResult](IDatabase database, Expression query, IModel model, Boolean async)
   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.<>c__DisplayClass9_0`1.<Execute>b__0()
   at Microsoft.EntityFrameworkCore.Query.Internal.CompiledQueryCache.GetOrAddQuery[TResult](Object cacheKey, Func`1 compiler)
   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.Execute[TResult](Expression query)
   at Microsoft.EntityFrameworkCore.Query.Internal.EntityQueryProvider.Execute[TResult](Expression expression)
   at Microsoft.EntityFrameworkCore.Query.Internal.EntityQueryable`1.GetEnumerator()
   at BugILikePostgres.Program.Main(String[] args) in C:\_Data\Repositories\BugILikePostgres\BugILikePostgres\Program.cs:line 73
```

- 2nd attempt, starting from [line 85](https://github.com/SommerEngineering/BugILikePostgres/blob/main/BugILikePostgres/Program.cs#L85): Using `Where(n => n.Topics.Contains(searchTerm2))` to find exact matches. Works fine, but users cannot search for partial words.

- 3rd attempt, starting from [line 105](https://github.com/SommerEngineering/BugILikePostgres/blob/main/BugILikePostgres/Program.cs#L105): This was our initial attempt by using `Where(n => n.Topics.Any(s => s.Contains(searchTerm3)))`, as already mentioned in [Github issue 395](https://github.com/npgsql/efcore.pg/issues/395#issuecomment-718807096). It throws an `The LINQ expression [...] could not be translated` exception. Stacktrace:

```
System.InvalidOperationException: The LINQ expression 'DbSet<Blog>()
    .Where(b => b.Name.Contains("Test"))
    .OrderBy(b => b.Id)
    .Where(b => b.Topics
        .Any(s => s.Contains(__searchTerm3_0)))' could not be translated. Either rewrite the query in a form that can be translated, or switch to client evaluation explicitly by ins
erting a call to 'AsEnumerable', 'AsAsyncEnumerable', 'ToList', or 'ToListAsync'. See https://go.microsoft.com/fwlink/?linkid=2101038 for more information.
   at Microsoft.EntityFrameworkCore.Query.QueryableMethodTranslatingExpressionVisitor.<VisitMethodCall>g__CheckTranslated|15_0(ShapedQueryExpression translated, <>c__DisplayClass15_
0& )
   at Microsoft.EntityFrameworkCore.Query.QueryableMethodTranslatingExpressionVisitor.VisitMethodCall(MethodCallExpression methodCallExpression)
   at System.Linq.Expressions.MethodCallExpression.Accept(ExpressionVisitor visitor)
   at System.Linq.Expressions.ExpressionVisitor.Visit(Expression node)
   at Microsoft.EntityFrameworkCore.Query.QueryCompilationContext.CreateQueryExecutor[TResult](Expression query)
   at Microsoft.EntityFrameworkCore.Storage.Database.CompileQuery[TResult](Expression query, Boolean async)
   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.CompileQueryCore[TResult](IDatabase database, Expression query, IModel model, Boolean async)
   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.<>c__DisplayClass9_0`1.<Execute>b__0()
   at Microsoft.EntityFrameworkCore.Query.Internal.CompiledQueryCache.GetOrAddQuery[TResult](Object cacheKey, Func`1 compiler)
   at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.Execute[TResult](Expression query)
   at Microsoft.EntityFrameworkCore.Query.Internal.EntityQueryProvider.Execute[TResult](Expression expression)
   at Microsoft.EntityFrameworkCore.Query.Internal.EntityQueryable`1.GetEnumerator()
   at BugILikePostgres.Program.Main(String[] args) in C:\_Data\Repositories\BugILikePostgres\BugILikePostgres\Program.cs:line 113
```

- 4th attempt, starting from [line 125](https://github.com/SommerEngineering/BugILikePostgres/blob/main/BugILikePostgres/Program.cs#L125): Following the introduction in the ["Array Type Mapping" documentation](https://www.npgsql.org/efcore/mapping/array.html) word-by-word, using `Where(n => n.Topics.Any(s => EF.Functions.ILike(searchTerm4b, s)))`. Uses the same approach as 1st attempt, but exchanging the `matchExpress` with the `pattern` (as the example in the documentation does it). Thus, it makes no sense, because we cannot use pattern anymore. Or do we miss something? This works fine, but the user cannot search for partial words. **This 4th attempt is interesting, because it shows, that there is no `null` involved. Thus, why 1st attempt throws an NPE, though?**

# Environment
- .NET 5.0 RC2
- Package Npgsql in version `5.0.0-preview1`
- Package Npgsql.EntityFrameworkCore.PostgreSQL in version `5.0.0-rc2`
- PostgreSQL 12.2 server, running on Windows 10 64bit
- Assumptions in the code, cf. [line 152](https://github.com/SommerEngineering/BugILikePostgres/blob/main/BugILikePostgres/Program.cs#L152):
    - Database host = `localhost`
    - Database port = `5432`
    - Database name = `BugILike`
    - Database user = `tester`
    - Database user's password = `test`