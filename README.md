# DeepCopy
Simple &amp; efficient library for deep copying .NET objects

Described in this blog post: https://reubenbond.github.io/posts/codegen-2-il-boogaloo

## Installation:
Install via NuGet:
```powershell
PM> Install-Package DeepCopy
```

## Usage:
```C#
// Add a using directive for DeepCopy.
var poco = new Poco();
var original = new[] { poco, poco };

var result = DeepCopier.Copy(original);

// The result is a copy of the original.
Assert.NotSame(original, result);

// Because both elements in the original array point to the same object, 
// both elements in the copied array also point to the same object.
Assert.Same(result[0], result[1]);
```

Optionally, classes can be marked using the `[Immutable]` attribute to tell `DeepCopy` to skip copying them and return them unmodified.
Object can also be wrapped in `Immutable<T>` using `Immutable.Create(value)`.

The majority of this project was adapted from [`dotnet/orleans`](https://github.com/dotnet/orleans).

PR's welcome!