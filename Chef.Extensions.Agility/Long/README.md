## Chef.Extensions.Long

### ToDateTime()

Parse Unix time milliseconds to DateTime.

Example:

    var milliseconds = 1557224635000;
    
    var time = milliseconds.ToDateTime();
    
    // time is 2019/05/07 10:23:55