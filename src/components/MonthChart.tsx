import { useState } from "react";
import "./MonthChart.css";

const monthNames = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
];

const books = (n: number) => `${n} ${n === 1 ? "book" : "books"}`;

type MonthChartProps = {
  months: number[];
};

function MonthChart({ months }: MonthChartProps) {
  const [active, setActive] = useState<number | null>(null);
  const peak = Math.max(0, ...months);
  const peakMonth = months.indexOf(peak);

  return (
    <figure className="month-chart">
      <div
        className="month-chart-plot"
        onPointerLeave={() => setActive(null)}
      >
        {months.map((count, index) => (
          <div
            key={monthNames[index]}
            className={`month-chart-column${active === index ? " active" : ""}`}
            role="img"
            tabIndex={0}
            aria-label={`${books(count)} in ${monthNames[index]}`}
            onPointerEnter={() => setActive(index)}
            onFocus={() => setActive(index)}
            onBlur={() => setActive(null)}
          >
            {active === index && (
              <span className="month-chart-tooltip" aria-hidden="true">
                {books(count)} in {monthNames[index]}
              </span>
            )}
            {index === peakMonth && peak > 0 && active !== index && (
              <span className="month-chart-value" aria-hidden="true">
                {count}
              </span>
            )}
            <span
              className="month-chart-bar"
              style={{ height: peak > 0 ? `${(count / peak) * 85}%` : 0 }}
            />
          </div>
        ))}
      </div>

      <div className="month-chart-axis" aria-hidden="true">
        {monthNames.map((name) => (
          <span key={name}>{name.slice(0, 3)}</span>
        ))}
      </div>

      <table className="sr-only">
        <caption>Books finished each month</caption>
        <thead>
          <tr>
            <th scope="col">Month</th>
            <th scope="col">Books</th>
          </tr>
        </thead>
        <tbody>
          {months.map((count, index) => (
            <tr key={monthNames[index]}>
              <th scope="row">{monthNames[index]}</th>
              <td>{count}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </figure>
  );
}

export default MonthChart;
