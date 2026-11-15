package hr.brendigo.clanarina

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.shape.RoundedCornerShape
import hr.brendigo.clanarina.ui.Brand
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import java.math.BigDecimal
import java.math.RoundingMode
import java.text.NumberFormat
import java.util.Locale

internal fun euroAmount(cents: Long) =
    NumberFormat.getCurrencyInstance(Locale.forLanguageTag("hr-HR")).format(BigDecimal.valueOf(cents, 2))

internal fun parseEuroAmount(input: String): Long =
    BigDecimal(input.trim().replace(",", ".")).setScale(2, RoundingMode.UNNECESSARY)
        .movePointRight(2).longValueExact()

@Composable internal fun Notice(text: String) {
    Card(
        modifier=Modifier.fillMaxWidth(),
        shape=RoundedCornerShape(16.dp),
        border=BorderStroke(1.dp,Brand.Stroke),
        colors=CardDefaults.cardColors(containerColor=Color.White)
    ) {
        Text(text,Modifier.fillMaxWidth().padding(20.dp),color=Brand.Ink)
    }
}
