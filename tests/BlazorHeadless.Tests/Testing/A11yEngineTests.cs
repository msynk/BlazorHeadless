using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace BlazorHeadless.Tests.Testing;

/// <summary>The audit rules, accessible-name computation and focus order of BlazorHeadless.Testing.</summary>
public class A11yEngineTests
{
    private static readonly HtmlParser Parser = new();

    private static IElement Body(string html) => Parser.ParseDocument($"<body>{html}</body>").Body!;

    private static IElement El(string html, string selector) => Body(html).QuerySelector(selector)!;

    private static string[] Rules(string html, BhA11yOptions? options = null)
        => BhA11y.Audit(Body(html), options).Violations.Select(v => v.RuleId).Distinct().ToArray();

    [Theory]
    [InlineData("duplicate-id", "<p id='a'>x</p><p id='a'>y</p>")]
    [InlineData("aria-valid-role", "<div role='buton'>x</div>")]
    [InlineData("aria-valid-attr-value", "<button aria-expanded='yes'>x</button>")]
    [InlineData("aria-allowed-attr", "<div role='img' aria-label='x' aria-checked='true'></div>")]
    [InlineData("aria-required-attr", "<div role='slider' tabindex='0' aria-label='Volume'></div>")]
    [InlineData("idref-exists", "<input aria-labelledby='nope'>")]
    [InlineData("aria-required-children", "<div role='listbox' aria-label='x'><button>not an option</button></div>")]
    [InlineData("aria-required-parent", "<div role='option' aria-selected='false'>x</div>")]
    [InlineData("accessible-name", "<button></button>")]
    [InlineData("aria-hidden-focus", "<div aria-hidden='true'><button>x</button></div>")]
    [InlineData("nested-interactive", "<button>a <a href='/x'>b</a></button>")]
    [InlineData("tabindex", "<button tabindex='3'>x</button>")]
    [InlineData("presentation-conflict", "<button role='presentation'>x</button>")]
    [InlineData("activedescendant-target", "<div role='listbox' aria-label='x' tabindex='0' aria-activedescendant='o'></div><div role='listbox' aria-label='y'><div role='option' id='o' aria-selected='false'>x</div></div>")]
    [InlineData("form-label", "<input type='text'>")]
    public void Rule_reports_its_violation(string rule, string html)
        => Assert.Contains(rule, Rules(html));

    [Theory]
    [InlineData("<button>Save</button>")]
    [InlineData("<label for='e'>Email</label><input id='e' type='email'>")]
    [InlineData("<label>Name <input></label>")]
    [InlineData("<div role='listbox' aria-label='Fruit' tabindex='0' aria-activedescendant='o1'><div role='option' id='o1' aria-selected='true'>Apple</div></div>")]
    [InlineData("<ul role='tree' aria-label='Files'><li role='treeitem' aria-expanded='false' tabindex='0'>src<ul role='group'><li role='treeitem' tabindex='-1'>a</li></ul></li></ul>")]
    [InlineData("<div aria-hidden='true'><button tabindex='-1'>x</button></div>")]
    [InlineData("<div role='slider' tabindex='0' aria-label='Volume' aria-valuenow='5' aria-valuemin='0' aria-valuemax='10'></div>")]
    [InlineData("<img src='a.png' alt=''>")]
    public void Valid_markup_passes(string html)
    {
        var report = BhA11y.Audit(Body(html));
        Assert.True(report.Passed, report.ToString());
    }

    [Fact]
    public void Fragments_ignore_references_outside_the_markup()
    {
        const string html = "<input aria-describedby='page-help' aria-label='x'>";
        Assert.Contains("idref-exists", Rules(html));
        Assert.DoesNotContain("idref-exists", Rules(html, new BhA11yOptions { IsFragment = true }));
    }

    [Fact]
    public void Rules_can_be_disabled_and_filtered_by_impact()
    {
        const string html = "<button></button><button tabindex='2'>x</button>";
        Assert.DoesNotContain("accessible-name", Rules(html, new BhA11yOptions().Without("accessible-name")));
        var serious = BhA11y.Audit(Body(html), new BhA11yOptions { MinimumImpact = BhA11yImpact.Critical });
        Assert.All(serious.Violations, v => Assert.Equal(BhA11yImpact.Critical, v.Impact));
    }

    [Fact]
    public void Report_renders_for_ci()
    {
        var report = BhA11y.Audit(Body("<button></button>"));
        Assert.False(report.Passed);
        Assert.Contains("accessible-name", report.ToMarkdown("Audit"));
        Assert.Contains("\"accessible-name\"", report.ToJson());
        var ex = new BhA11yException(report);
        Assert.Contains("accessible-name", ex.Message);
    }

    // ── Accessible name (accname 1.2) ────────────────────────────────────────

    [Theory]
    [InlineData("<button id='t'>Save</button>", "Save")]
    [InlineData("<button id='t' aria-label='Close'>×</button>", "Close")]
    [InlineData("<span id='l1'>Billing</span><span id='l2'>Address</span><div id='t' role='group' aria-labelledby='l1 l2'></div>", "Billing Address")]
    [InlineData("<label for='t'>Email</label><input id='t'>", "Email")]
    [InlineData("<label>Remember me <input id='t' type='checkbox'></label>", "Remember me")]
    [InlineData("<button id='t'><img src='i.png' alt='Print'> now</button>", "Print now")]
    [InlineData("<button id='t'>Delete <span aria-hidden='true'>🗑</span></button>", "Delete")]
    [InlineData("<input id='t' title='Search'>", "Search")]
    [InlineData("<input id='t' placeholder='Search…'>", "Search…")]
    [InlineData("<a id='t' href='/'>Home <span hidden>secret</span></a>", "Home")]
    [InlineData("<button id='t' aria-labelledby='t x'>Ok</button><span id='x'>dialog</span>", "Ok dialog")]
    [InlineData("<ul role='tree'><li id='t' role='treeitem'><span>src</span><ul role='group'><li role='treeitem'>app.cs</li></ul></li></ul>", "src")]
    public void Accessible_name(string html, string expected)
        => Assert.Equal(expected, El(html, "#t").AccessibleName());

    [Fact]
    public void Accessible_description_follows_describedby_then_title()
    {
        var body = Body("<p id='d'>Must be 8+ chars.</p><input id='a' aria-label='Password' aria-describedby='d' title='ignored'><input id='b' aria-label='Code' title='Six digits'>");
        Assert.Equal("Must be 8+ chars.", body.QuerySelector("#a")!.AccessibleDescription());
        Assert.Equal("Six digits", body.QuerySelector("#b")!.AccessibleDescription());
    }

    // ── Focus order ──────────────────────────────────────────────────────────

    [Fact]
    public void Tab_sequence_puts_positive_tabindex_first_and_skips_unfocusable()
    {
        var body = Body(
            "<a href='/a'>A</a>" +
            "<button disabled>Disabled</button>" +
            "<button tabindex='-1'>Programmatic</button>" +
            "<div hidden><button>Hidden</button></div>" +
            "<div inert><button>Inert</button></div>" +
            "<input type='hidden'>" +
            "<button tabindex='1'>First</button>" +
            "<div tabindex='0' role='button'>Div</div>" +
            "<input type='radio' name='r' checked aria-label='R1'><input type='radio' name='r' aria-label='R2'>");

        var names = BhFocusOrder.TabSequence(body).Select(s => s.Name).ToArray();
        Assert.Equal(["First", "A", "Div", "R1"], names);
    }

    [Fact]
    public void Tab_stop_describes_role_and_name()
    {
        var stops = BhFocusOrder.TabSequence(Body("<button>Save</button>"));
        Assert.Equal("button \"Save\"", stops[0].ToString());
        Assert.Contains("1. button \"Save\"", BhFocusOrder.Describe(stops));
    }
}
