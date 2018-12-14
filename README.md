# i18nBracketedSection
This is a way to get getext to talk in the same way as the i18n library which uses [[[ notation to lookup .po files.

The eventual aim of this project is to get a product that uses i18n square bracket 
notation to translate messages sent across a signalR pipe to clients on the end 
of a hub.

At the moment, this is standalone class only.

Requirements:

var text = @"A big long piece of text, populated with [[[some translatable bits]]], and some other
[[[bits with %0 parameters|||3]]], as well as existing [[[%0 translatable elements|||(((enuf)))]]].
It's worth noting that [[[it///context]]] and the other [[[it///cousin]]] are also handled.";

A function which takes a string and a context, and can translate it. I was thinking either 
HttpContext.Current.GetText or NGetText, but this contrived example works too:

Func<string, string, string> getTextWithContext = (s, ctx) => {
	if (s == "some translatable bits") return "Sum TranSLATEable bytes";
	if (s == "enuf") return "enough";
	if (ctx == "context") return "the context";
	if (ctx == "cousin") return "Cousin IT from the Adaams Family";
	return s;
};

Usage: 

var translated = i18nBracketedSection.FindAndTranslateBrackets(text, getTextWithContext);

=>

A big long piece of text, populated with Sum TranSLATEable bytes, and some other
bits with 3 parameters, as well as existing enough translatable elements.
It's worth noting that the context and the other Cousin IT from the Adaams Family are also handled.

