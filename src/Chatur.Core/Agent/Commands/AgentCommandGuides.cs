namespace Chatur.Core.Agent.Commands;

/// <summary>
/// The steps Chatur gives an agent for a guided command (BRD-249, REQ-FN-311): Chatur's own version of what the
/// TechieFlow task of the same name does, written for Chatur's tools (<c>read-file</c>, <c>edit-file</c>,
/// <c>run-build</c>) rather than TechieFlow's scripts. <c>{App}</c> is the project's name, <c>{args}</c> what the
/// person typed after the command, <c>{file}</c> the file the command writes.
/// </summary>
public static class AgentCommandGuides
{
    /// <summary>The steps for a guided command, with the project's name, the arguments and the file filled in.</summary>
    /// <param name="aCommand">The command.</param>
    /// <param name="aApp">The project's name.</param>
    /// <param name="aArguments">What the person typed after the command.</param>
    /// <param name="aFile">The file the command writes, relative to the project, or <see langword="null"/>.</param>
    /// <param name="aToday">Today's date, for the documents the command dates.</param>
    public static string For(AgentCommand aCommand, string aApp, string aArguments, string? aFile, DateTime aToday)
    {
        var vText = aCommand.Code switch
        {
            "brainstorm" => Brainstorm,
            "create-project-brief" => ProjectBrief,
            "research-prompt" or "research" => ResearchPrompt,
            "doc-out" => DocOut,
            "day1-brownfield" => Day1Brownfield,
            "devguide" => DevGuide,
            "productguide" => ProductGuide,
            "architect" => Architecture,
            "create-doc" => CreateDoc,
            _ => "Carry out *{code} for {App}: {description}"
        };

        return Common.Replace("{body}", vText, StringComparison.Ordinal)
            .Replace("{code}", aCommand.Code, StringComparison.Ordinal)
            .Replace("{description}", aCommand.Description, StringComparison.Ordinal)
            .Replace("{App}", aApp, StringComparison.Ordinal)
            .Replace("{args}", aArguments.Length == 0 ? "(nothing)" : aArguments, StringComparison.Ordinal)
            .Replace("{file}", aFile ?? "its file", StringComparison.Ordinal)
            .Replace("{today}", aToday.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    private const string Common = """
        The person typed the command *{code} (what followed it: {args}). You are carrying it out now, in Chatur, for the project {App}. Today is {today}. These are its steps; follow them in order and stay in this command until its file is written or the person types another command.

        {body}

        Rules for every command:
        - Write files only with the edit-file tool, paths relative to the project folder, the whole file each time. Read with read-file. Never run a source-control command.
        - Plain, simple English in everything the person reads. Ask questions as a short numbered list, all at once, and wait for the answers.
        - When the conversation is in "Go ahead" and the person did not answer a question, take the sensible reading, say which, and carry on.
        - Never say you are writing or have read something unless the same turn makes the tool call: a turn that ends with "I am writing it now" and no edit-file call leaves nothing on disk, and the person has to ask again.
        - To find files, call read-file on a folder, or on "." for the whole project: it lists what is in it. Then read the files you need, and write.
        - When the person has answered your questions (or the conversation is in "Go ahead"), the very next thing you do is call edit-file with the whole file; do not show the document in the chat first and ask for leave.
        - When the file is written, say so in one line naming {file}.
        """;

    private const string Brainstorm = """
        *brainstorm {topic}: a facilitated brainstorming session. You are the facilitator: draw the person's own ideas out with questions, prompts and examples; do not brainstorm for them unless they ask you to.
        1. Setup. Ask these four questions in one message, without saying what comes after: (1) What are we brainstorming about? (skip when the topic was given) (2) Any constraints or limits? (3) Broad exploration or focused ideas? (4) Shall I save the session as a document to read later? (the default is yes)
        2. Approach. Offer four numbered ways on: 1) you pick the techniques, 2) I recommend techniques for this topic, 3) random techniques for variety, 4) a progressive flow from broad to narrow.
        3. Techniques, one at a time: What if scenarios; Analogies from other products; First principles; SCAMPER (substitute, combine, adapt, modify, put to another use, eliminate, reverse); Role playing as each kind of user; Six thinking hats; Mind mapping; Five whys; Constraints flipped. Stay with one technique until the person wants to switch, build on their ideas ("yes, and"), and keep a running list of every idea in their own words.
        4. Flow: warm up, then many ideas without judging them, then group them, then pick and refine.
        5. When the person says they are done, or asks for the result or the brief, write {file} with: Summary (topic and goals, techniques used, how many ideas, the main themes); one section per technique (its ideas in the person's words, what was learnt); Ideas sorted into Immediate, Future, Moonshots and Insights; Action plan (the top three ideas, why, the next step for each); Reflection (what worked, what to explore next, open questions).
        6. Say the file is written and that *create-project-brief turns it into the brief.
        """;

    private const string ProjectBrief = """
        *create-project-brief: write {file}, the one page day 1 reads. It is the cheapest place to be wrong, so it is short and the person can correct it in two minutes.
        1. Read what there is: docs/brainstorming-session-results.md when it exists, any earlier brief, and this conversation.
        2. Fill five sections from it. Ask only for what you cannot work out, in one round of questions, all at once.
        3. Anything you do not know goes under "Open questions", never into a section as a guess.
        4. Every "Must do" line is an outcome a user gets, numbered, one per line; a line needing a paragraph is two lines.
        5. Write {file} exactly in this shape (edit it in place if it exists; never a -v2):
           # {App} — Brief
           | | |
           |---|---|
           | App | {App} |
           | Date | {today} |
           ## What it is  (two or three sentences, no technology, at most 150 words)
           ## Who it is for  (each kind of user and what they are trying to get done)
           ## Must do  (numbered outcomes)
           ## Out of scope  (what it deliberately does not do)
           ## Open questions  (one per line; leave the section out when there are none)
        6. Say in one line: the file, and how many open questions it carries. The next command is *day1-greenfield.
        """;

    private const string ResearchPrompt = """
        *research-prompt {topic}: write a deep research prompt someone (or a research tool) can run.
        1. Read the brief and the brainstorm results in docs/ when they exist.
        2. Offer the research focus as a numbered list and let the person pick: 1) product validation, 2) market opportunity, 3) users and customers, 4) competitors, 5) technology and innovation, 6) industry and ecosystem, 7) strategic options, 8) risks, 9) a custom focus.
        3. Ask what decisions the research should inform, the known constraints, and what is already known.
        4. Write {file} with: the research objective; background; the key questions (primary and secondary); the method (sources, analysis, data quality); the expected output (summary, detailed findings, appendices); success criteria; and the timeline.
        5. Write the file with edit-file, then say what it holds in two lines and ask whether anything should change; a change is written in place.
        """;

    private const string DocOut = """
        *doc-out: write the whole document drafted in this conversation to its file.
        1. Find the document you have been drafting here and the file it belongs in (the path the conversation used; else docs/ with a plain name).
        2. Write it whole with edit-file, nothing cut, nothing summarised.
        3. Say which file was written.
        """;

    private const string Day1Brownfield = """
        *day1-brownfield {App}: bring the documents into a project that already has code, in one flow, for the person to review at the end.
        1. Read the code and the markdown files in docs/ and at the root. Ask at most three questions at once: which documents to use, the stack choices the code leaves open, and the size (count the routed pages and the roles, propose Small up to 10 screens and 50 requirements, Medium up to 20 and 100, Large split into phases).
        2. Write docs/{App}-Architecture.md: stack decisions from the project files (cite the file), the solution structure, the component map from project references, the data model from the migrations or entities, and a decisions log opening with "current stack as-is".
        3. Write {file}: summary, scope, users and roles, a Screens and flow table with one row per routed page, then one BRD-n item per testable thing, each with its screen and one line "When <who> <does what> on <screen>, then <what they see>". Mark items read from the code as inferred.
        4. Write docs/{App}-Checklist.md: one REQ-UI-, REQ-FN- or REQ-NFR- row per BRD item, in a Requirements Status table with Status, %, Remarks; what the code already does is "Done (pre-existing)".
        5. Write docs/{App}-Coding-Standards.md from the style the code already follows, PROJECT-STATUS.md (where the project stands, the next step), and docs/{App}-UsageGuide.md (how to start it, one "How to test" per screen).
        6. Build once with run-build and note any error under the status's known blockers.
        7. List every file written, one line each.
        """;

    private const string DevGuide = """
        *devguide {App} [--update]: write {file}, the developer's map from each screen to the code that serves it, read at file and line so a developer can set a breakpoint.
        1. Find every routed page in the code (its @page or route), its file, the screen the UI design gives it and who reaches it. With --update, keep the entries whose files did not change.
        2. For each screen, read the code and write: the route and file; one "Call chain:" line from the page method to the service method to the data method; and a where-to-break table (file and line, the function, the variable to watch, the value it should hold). A path you cannot confirm is written "{unresolved — reason}", never invented.
        3. Build once with run-build; a screen you could not see running is marked "static-only (unconfirmed)".
        4. Write the file and say how many screens it documents and which paths are unresolved.
        """;

    private const string Architecture = """
        *architect {App}: design the solution and write {file}.
        1. Read the brief and the BRD in docs/, and the code when there is any.
        2. Ask, all at once, only what they do not answer: the stack, where the data lives, how people sign in, how it is hosted.
        3. Write {file}: stack decisions (one row per question, with where the answer came from); the solution structure (projects and folders); a component map; the data model (tables and their keys); cross-cutting concerns (configuration, secrets, logging, errors, tests); and a decisions log (date, decision, why, status).
        4. Say the file is written and list the open questions.
        """;

    private const string CreateDoc = """
        *create-doc {document}: write one project document.
        1. Ask which document when it was not named (a brief, a BRD, an architecture, a UI design, a usage guide), and read what docs/ already holds.
        2. Ask only what you cannot find, in one round.
        3. Write it under docs/ as docs/{App}-<Document>.md with its sections in the usual order, and say which file was written.
        """;

    private const string ProductGuide = """
        *productguide {App}: write {file}, the end user's manual.
        1. Read the BRD, the UI design and the UsageGuide in docs/, and the screens in the code.
        2. Write, in the user's words and never the developer's: what the product is for; getting started (install or open, sign in); then one section per screen in the order a user meets them — what it is for, what each control does, a short "How to" for each task, and what the empty, loading and error states look like; then common questions.
        3. Name a picture for each screen as docs/screenshots/{App}/<screen>.png where one exists.
        4. Write the file and say how many screens it covers.
        """;
}
