package hr.brendigo.clanarina

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import hr.brendigo.clanarina.ui.Brand

@Composable
internal fun StatCard(title:String, value:String, accent:Color, modifier:Modifier=Modifier) {
    Card(
        modifier=modifier.padding(vertical=4.dp).heightIn(min=104.dp),
        shape=RoundedCornerShape(16.dp),
        border=BorderStroke(1.dp,Brand.Stroke),
        colors=CardDefaults.cardColors(containerColor=Color.White)
    ) {
        Row(
            Modifier.fillMaxWidth().padding(horizontal=16.dp, vertical=18.dp),
            verticalAlignment=Alignment.CenterVertically,
            horizontalArrangement=Arrangement.spacedBy(14.dp)
        ) {
            Box(Modifier.width(5.dp).height(66.dp).background(accent, RoundedCornerShape(4.dp)))
            Column(verticalArrangement=Arrangement.spacedBy(7.dp)) {
                Text(title, color=Brand.Muted,
                    style=MaterialTheme.typography.bodyMedium,
                    fontWeight=FontWeight.Medium)
                Text(value, color=Brand.Navy,
                    style=MaterialTheme.typography.titleLarge,
                    fontWeight=FontWeight.Bold, softWrap=true)
            }
        }
    }
}

@Composable
internal fun Empty(text:String) {
    Card(
        modifier=Modifier.fillMaxWidth(),
        shape=RoundedCornerShape(16.dp),
        border=BorderStroke(1.dp,Brand.Stroke),
        colors=CardDefaults.cardColors(containerColor=Color.White)
    ) {
        Text(text, Modifier.padding(20.dp), color=Brand.Ink)
    }
}
