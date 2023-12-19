using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SourceBrowser.HtmlGenerator
{
    //
    // Summary:
    //     Describes the different types of glyphs that can be displayed in the default
    //     completion tool implementation.
    public enum StandardGlyphGroup
    {
        //
        // Summary:
        //     Describes symbols for classes.
        GlyphGroupClass = 0,
        //
        // Summary:
        //     Describes symbols for constants.
        GlyphGroupConstant = 6,
        //
        // Summary:
        //     Describes symbols for delegates.
        GlyphGroupDelegate = 12,
        //
        // Summary:
        //     Describes symbols for enumerations.
        GlyphGroupEnum = 18,
        //
        // Summary:
        //     Describes symbols for enumeration members.
        GlyphGroupEnumMember = 24,
        //
        // Summary:
        //     Describes symbols for events.
        GlyphGroupEvent = 30,
        //
        // Summary:
        //     Describes symbols for exceptions.
        GlyphGroupException = 36,
        //
        // Summary:
        //     Describes symbols for fields.
        GlyphGroupField = 42,
        //
        // Summary:
        //     Describes symbols for interfaces.
        GlyphGroupInterface = 48,
        //
        // Summary:
        //     Describes symbols for macros.
        GlyphGroupMacro = 54,
        //
        // Summary:
        //     Describes symbols for maps.
        GlyphGroupMap = 60,
        //
        // Summary:
        //     Describes symbols for map items.
        GlyphGroupMapItem = 66,
        //
        // Summary:
        //     Describes symbols for methods.
        GlyphGroupMethod = 72,
        //
        // Summary:
        //     Describes symbols for overloads.
        GlyphGroupOverload = 78,
        //
        // Summary:
        //     Describes symbols for modules.
        GlyphGroupModule = 84,
        //
        // Summary:
        //     Describes symbols for namespaces.
        GlyphGroupNamespace = 90,
        //
        // Summary:
        //     Describes symbols for operators.
        GlyphGroupOperator = 96,
        //
        // Summary:
        //     Describes symbols for properties.
        GlyphGroupProperty = 102,
        //
        // Summary:
        //     Describes symbols for structs.
        GlyphGroupStruct = 108,
        //
        // Summary:
        //     Describes symbols for templates.
        GlyphGroupTemplate = 114,
        //
        // Summary:
        //     Describes symbols for typedefs.
        GlyphGroupTypedef = 120,
        //
        // Summary:
        //     Describes symbols for types.
        GlyphGroupType = 126,
        //
        // Summary:
        //     Describes symbols for unions.
        GlyphGroupUnion = 132,
        //
        // Summary:
        //     Describes symbols for variables.
        GlyphGroupVariable = 138,
        //
        // Summary:
        //     Describes symbols for value types.
        GlyphGroupValueType = 144,
        //
        // Summary:
        //     Describes intrinsic symbols.
        GlyphGroupIntrinsic = 150,
        //
        // Summary:
        //     Describes symbols for J# methods.
        GlyphGroupJSharpMethod = 156,
        //
        // Summary:
        //     Describes symbols for J# fields.
        GlyphGroupJSharpField = 162,
        //
        // Summary:
        //     Describes symbols for J# classes.
        GlyphGroupJSharpClass = 168,
        //
        // Summary:
        //     Describes symbols for J# namespaces.
        GlyphGroupJSharpNamespace = 174,
        //
        // Summary:
        //     Describes symbols for J# interfaces.
        GlyphGroupJSharpInterface = 180,
        //
        // Summary:
        //     Describes symbols for errors.
        GlyphGroupError = 186,
        //
        // Summary:
        //     Describes symbols for BSC files.
        GlyphBscFile = 191,
        //
        // Summary:
        //     Describes symbols for assemblies.
        GlyphAssembly = 192,
        //
        // Summary:
        //     Describes symbols for libraries.
        GlyphLibrary = 193,
        //
        // Summary:
        //     Describes symbols for VB projects.
        GlyphVBProject = 194,
        //
        // Summary:
        //     Describes symbols for C# projects.
        GlyphCoolProject = 196,
        //
        // Summary:
        //     Describes symbols for C++ projects.
        GlyphCppProject = 199,
        //
        // Summary:
        //     Describes symbols for dialog identifiers.
        GlyphDialogId = 200,
        //
        // Summary:
        //     Describes symbols for open folders.
        GlyphOpenFolder = 201,
        //
        // Summary:
        //     Describes symbols for closed folders.
        GlyphClosedFolder = 202,
        //
        // Summary:
        //     Describes arrow symbols.
        GlyphArrow = 203,
        //
        // Summary:
        //     Describes symbols for C# files.
        GlyphCSharpFile = 204,
        //
        // Summary:
        //     Describes symbols for C# expansions.
        GlyphCSharpExpansion = 205,
        //
        // Summary:
        //     Describes symbols for keywords.
        GlyphKeyword = 206,
        //
        // Summary:
        //     Describes symbols for information.
        GlyphInformation = 207,
        //
        // Summary:
        //     Describes symbols for references.
        GlyphReference = 208,
        //
        // Summary:
        //     Describes symbols for recursion.
        GlyphRecursion = 209,
        //
        // Summary:
        //     Describes symbols for XML items.
        GlyphXmlItem = 210,
        //
        // Summary:
        //     Describes symbols for J# projects.
        GlyphJSharpProject = 211,
        //
        // Summary:
        //     Describes symbols for J# documents.
        GlyphJSharpDocument = 212,
        //
        // Summary:
        //     Describes symbols for forwarded types.
        GlyphForwardType = 213,
        //
        // Summary:
        //     Describes symbols for callers graphs.
        GlyphCallersGraph = 214,
        //
        // Summary:
        //     Describes symbols for call graphs.
        GlyphCallGraph = 215,
        //
        // Summary:
        //     Describes symbols for build warnings.
        GlyphWarning = 216,
        //
        // Summary:
        //     Describes symbols for something that may be a reference.
        GlyphMaybeReference = 217,
        //
        // Summary:
        //     Describes symbols for something that may be a caller.
        GlyphMaybeCaller = 218,
        //
        // Summary:
        //     Describes symbols for something that may be a call.
        GlyphMaybeCall = 219,
        //
        // Summary:
        //     Describes symbols for extension methods.
        GlyphExtensionMethod = 220,
        //
        // Summary:
        //     Describes symbols for internal extension methods.
        GlyphExtensionMethodInternal = 221,
        //
        // Summary:
        //     Describes symbols for friend extension methods.
        GlyphExtensionMethodFriend = 222,
        //
        // Summary:
        //     Describes symbols for protected extension methods.
        GlyphExtensionMethodProtected = 223,
        //
        // Summary:
        //     Describes symbols for private extension methods.
        GlyphExtensionMethodPrivate = 224,
        //
        // Summary:
        //     Describes symbols for extension method shortcuts.
        GlyphExtensionMethodShortcut = 225,
        //
        // Summary:
        //     Describes symbols for XML attributes.
        GlyphXmlAttribute = 226,
        //
        // Summary:
        //     Describes symbols for child XML elements.
        GlyphXmlChild = 227,
        //
        // Summary:
        //     Describes symbols for descendant XML elements.
        GlyphXmlDescendant = 228,
        //
        // Summary:
        //     Describes symbols for XML namespaces.
        GlyphXmlNamespace = 229,
        //
        // Summary:
        //     Describes symbols with a question mark for XML attributes.
        GlyphXmlAttributeQuestion = 230,
        //
        // Summary:
        //     Describes symbols with a check mark for XML attributes.
        GlyphXmlAttributeCheck = 231,
        //
        // Summary:
        //     Describes symbols with a question mark for XML child elements.
        GlyphXmlChildQuestion = 232,
        //
        // Summary:
        //     Describes symbols with a check mark for XML child elements.
        GlyphXmlChildCheck = 233,
        //
        // Summary:
        //     Describes symbols with a question mark for XML descendant elements.
        GlyphXmlDescendantQuestion = 234,
        //
        // Summary:
        //     Describes symbols with a check mark for XML descendant elements.
        GlyphXmlDescendantCheck = 235,
        //
        // Summary:
        //     Describes symbols for completion warnings.
        GlyphCompletionWarning = 236,
        //
        // Summary:
        //     Describes symbols for unknown types.
        GlyphGroupUnknown = 237
    }

    public enum StandardGlyphItem
    {
        //
        // Summary:
        //     Describes a public symbol.
        GlyphItemPublic,
        //
        // Summary:
        //     Describes an internal symbol.
        GlyphItemInternal,
        //
        // Summary:
        //     Describes a friend symbol.
        GlyphItemFriend,
        //
        // Summary:
        //     Describes a protected symbol.
        GlyphItemProtected,
        //
        // Summary:
        //     Describes a private symbol.
        GlyphItemPrivate,
        //
        // Summary:
        //     Describes a shortcut symbol.
        GlyphItemShortcut,
        //
        // Summary:
        //     Describes a symbol that has all (or none) of the standard attributes.
        TotalGlyphItems
    }
}
