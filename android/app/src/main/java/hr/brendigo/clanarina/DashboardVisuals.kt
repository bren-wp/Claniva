package hr.brendigo.clanarina

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import hr.brendigo.clanarina.data.LedgerEntry
import hr.brendigo.clanarina.data.Totals
import hr.brendigo.clanarina.ui.Brand
import java.util.Locale

/** Native graph from actual SQLite payments, not a mock illustration. */
@Composable
internal fun MonthlyPaymentChart(
    payments: List<LedgerEntry>,
    modifier: Modifier = Modifier
) {
    val months = remember(payments) { recentSixMonths(payments) }
    Card(
        modifier = modifier,
        shape = RoundedCornerShape(16.dp),
        border = BorderStroke(1.dp, Brand.Stroke),
        colors = CardDefaults.cardColors(containerColor = Color.White)
    ) {
        Column(
            modifier = Modifier.fillMaxWidth().padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text(
                "Uplate po mjesecima",
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold,
                color = Brand.Navy
            )
            val maxCents = months.maxOf { it.cents }.coerceAtLeast(1L)
            Canvas(Modifier.fillMaxWidth().height(152.dp)) {
                val left = 10.dp.toPx()
                val right = size.width - 10.dp.toPx()
                val top = 12.dp.toPx()
                val bottom = size.height - 16.dp.toPx()
                val dx = (right - left) / 5
                drawLine(
                    color = Brand.Stroke,
                    start = Offset(left, bottom),
                    end = Offset(right, bottom),
                    strokeWidth = 1.5.dp.toPx()
                )
                val coords = months.mapIndexed { index, value ->
                    Offset(
                        x = left + dx * index,
                        y = bottom - (bottom - top) * value.cents.toFloat() / maxCents.toFloat()
                    )
                }
                val path = Path().apply {
                    coords.forEachIndexed { index, point ->
                        if (index == 0) moveTo(point.x, point.y)
                        else lineTo(point.x, point.y)
                    }
                }
                drawPath(path, color = Brand.Teal,
                    style = Stroke(width = 3.dp.toPx(), cap = StrokeCap.Round, join = StrokeJoin.Round))
                coords.forEach { point ->
                    drawCircle(Brand.Teal, radius = 4.dp.toPx(), center = point)
                }
            }
            Row(Modifier.fillMaxWidth()) {
                months.forEach { month ->
                    Text(
                        month.label,
                        modifier = Modifier.weight(1f),
                        color = Brand.Muted,
                        style = MaterialTheme.typography.labelSmall,
                        maxLines = 1
                    )
                }
            }
            Text(
                if (months.all { it.cents == 0L })
                    "Nema evidentiranih uplata u posljednjih šest mjeseci."
                else "Evidentirane uplate · nije bankovna potvrda.",
                color = Brand.Muted, style = MaterialTheme.typography.bodySmall
            )
        }
    }
}

/** Visual ratio of charged amounts covered per member; not bank confirmation. */
@Composable
internal fun CollectionStatusCard(totals: Totals, modifier: Modifier = Modifier) {
    val coverage = coveredPercent(totals).coerceIn(0f, 100f)
    val paidAgainstCharges = (totals.chargedCents - totals.outstandingCents)
        .coerceIn(0L, totals.chargedCents.coerceAtLeast(0L))
    Card(
        modifier = modifier,
        shape = RoundedCornerShape(16.dp),
        border = BorderStroke(1.dp, Brand.Stroke),
        colors = CardDefaults.cardColors(containerColor = Color.White)
    ) {
        Column(
            modifier = Modifier.fillMaxWidth().padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                "Status članarina",
                modifier = Modifier.fillMaxWidth(),
                color = Brand.Navy,
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold
            )
            Box(Modifier.size(156.dp), contentAlignment = Alignment.Center) {
                Canvas(Modifier.fillMaxSize()) {
                    val stroke = 19.dp.toPx()
                    val corner = Stroke(width = stroke, cap = StrokeCap.Round)
                    val inset = stroke / 2 + 2.dp.toPx()
                    val arcSize = androidx.compose.ui.geometry.Size(
                        size.width - 2 * inset, size.height - 2 * inset
                    )
                    val offset = Offset(inset, inset)
                    drawArc(Brand.Stroke, -90f, 360f, false, topLeft = offset,
                        size = arcSize, style = Stroke(width = stroke))
                    if (coverage > 0f) {
                        drawArc(Brand.Teal, -90f, coverage * 3.6f, false,
                            topLeft = offset, size = arcSize, style = corner)
                    }
                }
                Text(
                    String.format(Locale.forLanguageTag("hr-HR"), "%.1f %%", coverage),
                    style = MaterialTheme.typography.headlineSmall,
                    color = Brand.Navy,
                    fontWeight = FontWeight.Bold
                )
            }
            Column(
                Modifier.fillMaxWidth(),
                verticalArrangement = Arrangement.spacedBy(7.dp)
            ) {
                Text("Pokriveno zaduženje: " + euro(paidAgainstCharges),
                    color = Brand.Ink, fontWeight = FontWeight.SemiBold)
                Text("Nepodmireno: " + euro(totals.outstandingCents),
                    color = Brand.Danger)
                Text("Preplate: " + euro(totals.creditCents),
                    color = Brand.Ink)
            }
            Text(
                if (totals.chargedCents == 0L)
                    "Nema zaduženja za obračun postotka."
                else "Status iz lokalne evidencije, nije bankovna potvrda.",
                modifier = Modifier.fillMaxWidth(),
                color = Brand.Muted,
                style = MaterialTheme.typography.bodySmall
            )
        }
    }
}
