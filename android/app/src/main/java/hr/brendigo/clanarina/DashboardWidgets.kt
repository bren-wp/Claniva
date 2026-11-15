package hr.brendigo.clanarina

import androidx.compose.foundation.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.res.painterResource
import hr.brendigo.clanarina.data.LocalStore
import hr.brendigo.clanarina.ui.Brand
import java.math.BigDecimal
import java.text.NumberFormat
import java.util.Locale

@Composable internal fun StatCard(title:String,value:String,accent:Color,modifier:Modifier=Modifier) {
    Card(modifier=modifier.padding(vertical=4.dp),shape=RoundedCornerShape(16.dp),colors=CardDefaults.cardColors(containerColor=Color.White)) {
        Column(Modifier.padding(15.dp)) {
            Text(title,color=accent,style=MaterialTheme.typography.bodySmall,fontWeight=FontWeight.SemiBold)
            Spacer(Modifier.height(8.dp))
            Text(value,style=MaterialTheme.typography.titleLarge,fontWeight=FontWeight.Bold)
        }
    }
}
@Composable internal fun Empty(text:String) {
    Card(colors=CardDefaults.cardColors(containerColor=Color.White)) {
        Text(text,Modifier.fillMaxWidth().padding(22.dp))
    }
}
