using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// An image element with a graceful fallback for representing a user or entity.
/// A faithful port of Radix UI's <c>Avatar</c> primitive.
///
/// <para><b>Compound API:</b></para>
/// <list type="bullet">
///   <item><see cref="BhAvatar"/> — the root container (renders a <c>&lt;span&gt;</c>).</item>
///   <item><see cref="BhAvatarImage"/> — the image; only shown once it loads successfully.</item>
///   <item><see cref="BhAvatarFallback"/> — rendered until the image loads (or if it errors), with an optional delay.</item>
/// </list>
///
/// <para><b>Key features (inspired by Radix UI):</b></para>
/// <list type="bullet">
///   <item>
///     <b>Automatic fallback</b> — the image is only displayed after it loads,
///     avoiding broken-image icons. While loading or on error the fallback shows.
///   </item>
///   <item>
///     <b>Loading-status coordination</b> — the root tracks the image's
///     <see cref="BhAvatarImageLoadingStatus"/> and shares it via cascading
///     context so the fallback knows when to hide.
///   </item>
///   <item>
///     <b>Polymorphic rendering</b> — renders a <c>&lt;span&gt;</c> by default;
///     set <see cref="BhComponentBase.As"/> to render as any element.
///   </item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhAvatar class="avatar"&gt;
///     &lt;BhAvatarImage Src="/user.jpg" Alt="Jane Doe" class="avatar-image" /&gt;
///     &lt;BhAvatarFallback DelayMs="600" class="avatar-fallback"&gt;JD&lt;/BhAvatarFallback&gt;
/// &lt;/BhAvatar&gt;
/// </code>
/// </summary>
public class BhAvatar : BhComponentBase
{
    private BhAvatarImageLoadingStatus _status = BhAvatarImageLoadingStatus.Idle;

    /// <summary>
    /// The avatar content — typically a <see cref="BhAvatarImage"/> and a
    /// <see cref="BhAvatarFallback"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The root renders a <c>&lt;span&gt;</c> by default, matching Radix.</summary>
    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BhAvatarContext
        {
            Status = _status,
            SetStatus = SetStatus,
        };

        builder.OpenComponent<CascadingValue<BhAvatarContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());

            if (Ref is not null)
                inner.AddElementReferenceCapture(30, Ref);

            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    private void SetStatus(BhAvatarImageLoadingStatus status)
    {
        if (_status == status) return;
        _status = status;
        StateHasChanged();
    }
}
