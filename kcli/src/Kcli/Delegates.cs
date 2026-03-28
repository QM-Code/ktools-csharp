namespace Kcli;

public delegate void FlagHandler(HandlerContext context);
public delegate void ValueHandler(HandlerContext context, string value);
public delegate void PositionalHandler(HandlerContext context);
