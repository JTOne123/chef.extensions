## Chef.Extensions.Double

### Round(MidpointRounding mode = MidpointRounding.AwayFromZero)

Round a int away from zero.

Example:

    var num1 = 1.4d.Round();
    var num2 = 1.5d.Round();
    var num3 = 1.6d.Round();
    
    // num1 = 1, num2 = 2, num3 = 2

### Round(int digits, MidpointRounding mode = MidpointRounding.AwayFromZero)

Round a double to `digits` places away from zero.

Example:

    var num1 = 1.44d.Round(1);
    var num2 = 1.55d.Round(1);
    var num3 = 1.66d.Round(1);
    
    // num1 = 1.4, num2 = 1.6, num3 = 1.7

### RoundUp()

Round up a double to int.

Example:

    var result = 1.44d.RoundUp();

    // result is 2.

### RoundUp(int digits)

Round up a double to `digits` places.

Example:

    var num1 = 1.44d.RoundUp(1);
    var num2 = 1.55d.RoundUp(1);
    var num3 = 1.66d.RoundUp(1);
    
    // num1 = 1.5, num2 = 1.6, num3 = 1.7

### RoundDown()

Round down a double to int.

Example:

    var result = 1.44d.RoundDown();

    // result is 1.

### RoundDown(int digits)

Round down a double to `digits` places.

Example:

    var num1 = 1.44d.RoundDown(1);
    var num2 = 1.55d.RoundDown(1);
    var num3 = 1.66d.RoundDown(1);
    
    // num1 = 1.4, num2 = 1.5, num3 = 1.6

### ToInt32()

Convert a double to int.

Example:

    var num1 = 1.4d.ToInt32();
    var num2 = 1.5d.ToInt32();
    var num3 = 1.6d.ToInt32();
    
    // num1 = 1, num2 = 2, num3 = 2

### ToInt64()

Convert a double to long.

Example:

    var num1 = 1.4d.ToInt64();
    var num2 = 1.5d.ToInt64();
    var num3 = 1.6d.ToInt64();
    
    // num1 = 1, num2 = 2, num3 = 2

### Gradient(double baseValue)

Calculate change rate with baseValue.

Example:

    var num = 11d;
    var baseValue = 10d;
    var result = num.Gradient(baseValue);
    
    // result is 0.1
