// Tooltips of the charts rendered by Meziantou.Framework.Charts (see BalanceHistoryChartBuilder.cs).
// Each point holds its values in data-* attributes. Blazor re-renders the SVG at any time, so the
// listeners are on the document and the points are prepared when they are added to the page.
(function () {
  const pointSelector = ".chart .chart-points > [data-series]";
  const numberFormat = new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  let tooltip = null;

  function element(name, className, ...children) {
    const result = document.createElement(name);
    if (className) {
      result.className = className;
    }

    result.append(...children);
    return result;
  }

  function formatAmount(value, currency, signed) {
    const text = numberFormat.format(Math.abs(value));
    const sign = value < 0 ? "-" : signed && value > 0 ? "+" : "";
    return (currency ? currency + " " : "") + sign + text;
  }

  function amountElement(value, currency, signed) {
    const className = value < 0 ? "amount-negative" : value > 0 && signed ? "amount-positive" : "";
    return element("dd", className, formatAmount(value, currency, signed));
  }

  // The title becomes the accessible name of the point, so the browser no longer shows its own
  // tooltip, and the point can be reached with the keyboard.
  function preparePoints() {
    for (const point of document.querySelectorAll(pointSelector + ":not([tabindex])")) {
      const title = point.querySelector("title");
      if (title) {
        point.setAttribute("aria-label", title.textContent);
        title.remove();
      }

      point.setAttribute("tabindex", "0");
    }
  }

  function show(point, x, y) {
    if (!tooltip) {
      tooltip = element("div", "chart-tooltip");
      tooltip.setAttribute("role", "tooltip");
      document.body.append(tooltip);
    }

    const data = point.dataset; // data-series, data-date, data-amount, data-change, data-currency
    const swatch = element("span", "chart-tooltip-swatch");
    swatch.style.background = point.getAttribute("fill");

    const details = element("dl", null,
      element("dt", null, "Date"), element("dd", null, data.date),
      element("dt", null, "Balance"), amountElement(Number(data.amount), data.currency, false));
    if (data.change !== undefined) {
      details.append(element("dt", null, "Change"), amountElement(Number(data.change), data.currency, true));
    }

    tooltip.replaceChildren(element("header", null, swatch, data.series), details);
    tooltip.hidden = false;

    // Next to the pointer, flipped when it would leave the window
    const { width, height } = tooltip.getBoundingClientRect();
    const left = x + 12 + width > innerWidth ? x - 12 - width : x + 12;
    const top = y + 12 + height > innerHeight ? y - 12 - height : y + 12;
    tooltip.style.left = `${Math.max(0, left)}px`;
    tooltip.style.top = `${Math.max(0, top)}px`;
  }

  function hide() {
    if (tooltip) {
      tooltip.hidden = true;
    }
  }

  document.addEventListener("pointermove", event => {
    const point = event.target instanceof Element ? event.target.closest(pointSelector) : null;
    if (point) {
      show(point, event.clientX, event.clientY);
    } else {
      hide();
    }
  });

  document.addEventListener("focusin", event => {
    const point = event.target instanceof Element ? event.target.closest(pointSelector) : null;
    if (point) {
      const box = point.getBoundingClientRect();
      show(point, box.right, box.top);
    } else {
      hide();
    }
  });

  document.addEventListener("focusout", hide);
  document.addEventListener("scroll", hide, true);

  let pending = false;
  new MutationObserver(() => {
    if (!pending) {
      pending = true;
      requestAnimationFrame(() => {
        pending = false;
        preparePoints();
        if (tooltip && !tooltip.hidden && !document.querySelector(pointSelector + ":hover, " + pointSelector + ":focus")) {
          hide();
        }
      });
    }
  }).observe(document.documentElement, { childList: true, subtree: true });
})();
