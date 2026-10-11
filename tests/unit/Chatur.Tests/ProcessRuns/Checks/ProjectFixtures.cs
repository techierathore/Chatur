using Chatur.Tests.Support;

namespace Chatur.Tests.ProcessRuns.Checks;

/// <summary>Documents a project could hold, used to fill a <see cref="TempProject"/>.</summary>
public static class ProjectFixtures
{
    /// <summary>A brief naming an audience, a problem and a measure of success.</summary>
    public const string GoodBrief = "# Brief\n\nThe audience is small shops. The problem is stock counting. The measure of success is a count in five minutes.\n";

    /// <summary>A BRD of two items, each with an acceptance line.</summary>
    public const string GoodBrd =
        "# X — BRD\n\n"
        + "- **BRD-1** — Sign in. *Screen:* Sign in\n  - *Acceptance:* When the owner signs in, then the start window opens.\n"
        + "- **BRD-2** — Register. *Screen:* Sign in\n  - *Acceptance:* When the owner registers, then an account exists.\n";

    /// <summary>A checklist with one UI and one FN row for the two BRD items.</summary>
    public const string GoodChecklist =
        "# X — Checklist\n\n## Requirements Status\n\n"
        + "| ID | Requirement | Status | % | Remarks | Details |\n|----|-------------|--------|---|---------|---------|\n"
        + "| REQ-UI-001 | Sign in | Not Started | 0% | | |\n| REQ-FN-001 | Account | Not Started | 0% | | |\n\n"
        + "## Page: Sign in (`/signin`)\n\n"
        + "- **REQ-UI-001** — Sign in. *BRD:* BRD-1 · *Mockup:* mockups/sign-in.html\n  - *Acceptance:* When the owner signs in, then the start window opens.\n"
        + "- **REQ-FN-001** — Account. *BRD:* BRD-2\n  - *Acceptance:* When the owner registers, then an account exists.\n";

    /// <summary>A UI design with one fully described screen.</summary>
    public const string GoodUiDesign =
        "# X — UI design\n\n## Screens\n\n### Screen: Sign in (`/signin`)\n\n**Mockup:** [mockups/sign-in.html](mockups/sign-in.html)\n\n"
        + "| Region | Control | Shows |\n|---|---|---|\n| form | Card | the fields |\n\n**States:** empty: none · loading: a spinner · error: the message\n";

    /// <summary>A mockup with an anchor.</summary>
    public const string GoodMockup = "<html><body><form data-testid=\"signin-form\"></form></body></html>";

    /// <summary>Writes the documents a finished day-1 project holds.</summary>
    /// <param name="aProject">The project.</param>
    public static TempProject FullDay1(TempProject aProject) => aProject
        .Write("docs/X-Brief.md", GoodBrief)
        .Write("docs/X-BRD.md", GoodBrd)
        .Write("docs/X-Architecture.md", "# Architecture\n")
        .Write("docs/X-UIDesign.md", GoodUiDesign)
        .Write("docs/mockups/sign-in.html", GoodMockup)
        .Write("docs/X-Checklist.md", GoodChecklist);
}
