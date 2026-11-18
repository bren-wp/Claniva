package hr.brendigo.clanarina

import hr.brendigo.clanarina.data.LedgerEntry
import hr.brendigo.clanarina.data.Totals
import java.time.YearMonth
import java.time.format.TextStyle
import java.util.Locale

internal data class MonthlyPayment(val label: String, val cents: Long)

/** Never treats locally recorded payments as verified bank transactions. */
internal fun coveredPercent(totals: Totals): Float {
    if (totals.chargedCents <= 0L) return 0f
    val covered = (totals.chargedCents - totals.outstandingCents)
        .coerceIn(0L, totals.chargedCents)
    return 100f * covered.toFloat() / totals.chargedCents.toFloat()
}

internal fun recentSixMonths(
    payments: List<LedgerEntry>,
    now: YearMonth = YearMonth.now()
): List<MonthlyPayment> {
    val buckets = LongArray(6)
    val first = now.minusMonths(5)
    payments.forEach { payment ->
        if (payment.kind != "payment" || payment.amountCents <= 0L ||
            payment.createdAt.length < 7) return@forEach
        val month = runCatching { YearMonth.parse(payment.createdAt.take(7)) }.getOrNull()
            ?: return@forEach
        val index = (month.year - first.year) * 12 + month.monthValue - first.monthValue
        if (index in 0..5) buckets[index] = Math.addExact(buckets[index], payment.amountCents)
    }
    val croatian = Locale.forLanguageTag("hr-HR")
    return (0 until 6).map { index ->
        MonthlyPayment(
            first.plusMonths(index.toLong()).month
                .getDisplayName(TextStyle.SHORT, croatian).removeSuffix("."),
            buckets[index]
        )
    }
}
