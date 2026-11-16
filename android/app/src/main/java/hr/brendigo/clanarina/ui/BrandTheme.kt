package hr.brendigo.clanarina.ui
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

object Brand {
    val Navy = Color(0xFF0F1F3A)
    val Blue = Color(0xFF2563EB)
    val Teal = Color(0xFF14B8A6)
    val Gold = Color(0xFFF4B942)
    val Background = Color(0xFFF5F7FB)
    val Danger = Color(0xFFD63C43)
    val Ink = Color(0xFF17243A)
    val Muted = Color(0xFF64748B)
    val Stroke = Color(0xFFDCE5F0)
    val Success = Color(0xFF0B946E)
    val SuccessInk = Color(0xFF087358)
}
private val colors = lightColorScheme(
    primary=Brand.Blue, secondary=Brand.Teal, background=Brand.Background,
    surface=Color.White, onSurface=Brand.Ink, onBackground=Brand.Ink,
    onPrimary=Color.White, onSecondary=Brand.Navy,
    surfaceContainer=Color.White, surfaceContainerLow=Color.White,
    surfaceContainerHigh=Color.White, surfaceContainerHighest=Color.White,
    onSurfaceVariant=Brand.Muted, outline=Brand.Stroke,
    outlineVariant=Brand.Stroke, surfaceTint=Color.White,
    error=Brand.Danger
)
@Composable fun ClanarinaTheme(content: @Composable () -> Unit) {
    MaterialTheme(colorScheme=colors, content=content)
}
