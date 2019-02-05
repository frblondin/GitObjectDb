Validation
==========

Model can be validated using the `Validate` method:

.. code-block:: csharp

    repository.Validate(ValidationRules.All);

Extend validation using ICustomValidation
-----------------------

One way to perform validations on a model type is to make it implementing `ICustomValidation`.

.. code-block:: csharp

    [Model]
    public class Person : ICustomValidation
    {
        [Modifiable]
        public int Age { get; }

        public IEnumerable<ValidationFailure> Validate(ValidationContext context)
        {
            if (Age < 18)
            {
                yield return new ValidationFailure(nameof(Age), "Person should be older than 18.", context);
            }
        }
    }

Extend validation using IPropertyValidator
------------------------

A more advanced way to create validator that can apply to any model property is to create a new type implementing `IPropertyValidator`.

1. Create a new type:

.. code-block:: csharp

    [Model]
    public class SomePropertyValidator : IPropertyValidator
    {
        public bool CanValidate(Type type) => type == typeof(SomeType);

        public IEnumerable<ValidationFailure> Validate(
            string propertyName, object value, ValidationContext context)
        {
            var instance = (SomeType)value;
            if (instance.Gender == Gender.Male && instance.IsPregnant)
            {
                yield return new ValidationFailure(
                    nameof(IsPregnant),
                    "A male person cannot possibly be pregnant, not yet.",
                    context);
            }
        }
    }

2. Inject this type into dependency injection:

.. code-block:: csharp

    var serviceProvider = new ServiceCollection()
        .AddGitObjectDb()
        .AddSingleton<IPropertyValidator, DependencyPropertyValidator>()
        .BuildServiceProvider();
